using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.NopwaraWebhooks.Domain;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Services;

/// <summary>
/// Webhook service: subscriptions, queueing, signing and delivery with retries
/// </summary>
public class WebhookService
{
    #region Fields

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    //deliveries queued during this request by deduplication key (e.g. a product saved twice in one admin request)
    private readonly Dictionary<string, List<WebhookDelivery>> _queuedThisRequest = new();

    private readonly IRepository<WebhookDelivery> _deliveryRepository;
    private readonly IRepository<WebhookSubscription> _subscriptionRepository;
    private readonly NopwaraWebhooksSettings _settings;
    private readonly WebhookHttpClient _httpClient;

    #endregion

    #region Ctor

    public WebhookService(IRepository<WebhookDelivery> deliveryRepository,
        IRepository<WebhookSubscription> subscriptionRepository,
        NopwaraWebhooksSettings settings,
        WebhookHttpClient httpClient)
    {
        _deliveryRepository = deliveryRepository;
        _subscriptionRepository = subscriptionRepository;
        _settings = settings;
        _httpClient = httpClient;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets the signature header value: "sha256=" + lowercase hex HMAC-SHA256 of the raw body with the subscription secret
    /// </summary>
    public static string Sign(string secret, string body)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret ?? string.Empty), Encoding.UTF8.GetBytes(body));
        return "sha256=" + Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Generates a random secret
    /// </summary>
    public static string GenerateSecret()
    {
        return "whsec_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24));
    }

    private static bool Receives(WebhookSubscription subscription, string eventName)
    {
        var events = (subscription.Events ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return events.Contains(WebhookEvents.ALL) || events.Contains(eventName, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Subscriptions

    public async Task<IPagedList<WebhookSubscription>> SearchSubscriptionsAsync(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _subscriptionRepository.GetAllPagedAsync(query => query.OrderBy(s => s.Name), pageIndex, pageSize);
    }

    public async Task<WebhookSubscription> GetSubscriptionByIdAsync(int id)
    {
        return await _subscriptionRepository.GetByIdAsync(id);
    }

    public async Task InsertSubscriptionAsync(WebhookSubscription subscription)
    {
        await _subscriptionRepository.InsertAsync(subscription, false);
    }

    public async Task UpdateSubscriptionAsync(WebhookSubscription subscription)
    {
        await _subscriptionRepository.UpdateAsync(subscription, false);
    }

    public async Task DeleteSubscriptionAsync(WebhookSubscription subscription)
    {
        await _deliveryRepository.DeleteAsync(d => d.SubscriptionId == subscription.Id);
        await _subscriptionRepository.DeleteAsync(subscription, false);
    }

    #endregion

    #region Deliveries

    public async Task<IPagedList<WebhookDelivery>> SearchDeliveriesAsync(int subscriptionId = 0, int? statusId = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _deliveryRepository.GetAllPagedAsync(query =>
        {
            if (subscriptionId > 0)
                query = query.Where(d => d.SubscriptionId == subscriptionId);
            if (statusId.HasValue)
                query = query.Where(d => d.StatusId == statusId.Value);

            return query.OrderByDescending(d => d.Id);
        }, pageIndex, pageSize);
    }

    public async Task<WebhookDelivery> GetDeliveryByIdAsync(int id)
    {
        return await _deliveryRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Queues an event for every active subscription that receives it
    /// </summary>
    /// <param name="eventName">Event name (see WebhookEvents)</param>
    /// <param name="data">Event data (serialized as camelCase JSON)</param>
    /// <param name="storeId">Store the event belongs to (0 = not store specific)</param>
    /// <param name="dedupeKey">When set, the same event with the same key in one request updates the queued deliveries (latest data wins) instead of adding new ones</param>
    public async Task QueueAsync(string eventName, object data, int storeId = 0, string dedupeKey = null)
    {
        if (!_settings.Enabled)
            return;

        var key = dedupeKey == null ? null : $"{eventName}:{dedupeKey}";
        if (key != null && _queuedThisRequest.TryGetValue(key, out var queued))
        {
            foreach (var queuedDelivery in queued)
            {
                //re-read: the delivery task may have sent it in the meantime (then it must not be reset)
                var delivery = await _deliveryRepository.GetByIdAsync(queuedDelivery.Id, cache => default);
                if (delivery is not { Status: WebhookDeliveryStatus.Pending, Attempts: 0 })
                    continue;

                delivery.Payload = SerializeEnvelope(delivery, data, storeId);
                await _deliveryRepository.UpdateAsync(delivery, false);
            }

            return;
        }

        var created = new List<WebhookDelivery>();
        var subscriptions = await _subscriptionRepository.GetAllAsync(query => query.Where(s => s.IsActive));
        foreach (var subscription in subscriptions.Where(s => Receives(s, eventName) && (s.StoreId == 0 || storeId == 0 || s.StoreId == storeId)))
            created.Add(await CreateDeliveryAsync(subscription, eventName, data, storeId));

        if (key != null)
            _queuedThisRequest[key] = created;
    }

    private static string SerializeEnvelope(WebhookDelivery delivery, object data, int storeId)
    {
        return JsonSerializer.Serialize(new
        {
            id = delivery.DeliveryGuid,
            type = delivery.Event,
            createdUtc = delivery.CreatedOnUtc,
            storeId,
            data
        }, _jsonOptions);
    }

    /// <summary>
    /// Creates a pending delivery (the envelope is the same for every event)
    /// </summary>
    public async Task<WebhookDelivery> CreateDeliveryAsync(WebhookSubscription subscription, string eventName, object data, int storeId)
    {
        var now = DateTime.UtcNow;
        var delivery = new WebhookDelivery
        {
            DeliveryGuid = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            Event = eventName,
            Status = WebhookDeliveryStatus.Pending,
            NextAttemptUtc = now,
            CreatedOnUtc = now
        };
        delivery.Payload = SerializeEnvelope(delivery, data, storeId);

        await _deliveryRepository.InsertAsync(delivery, false);

        return delivery;
    }

    /// <summary>
    /// Sends one delivery and records the result (retry schedule on failure)
    /// </summary>
    public async Task<bool> SendAsync(WebhookDelivery delivery)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(delivery.SubscriptionId);
        if (subscription == null)
            return false;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var headers = new Dictionary<string, string>
        {
            ["X-Nopwara-Event"] = delivery.Event,
            ["X-Nopwara-Delivery"] = delivery.DeliveryGuid.ToString(),
            ["X-Nopwara-Timestamp"] = timestamp,
            ["X-Nopwara-Signature"] = Sign(subscription.Secret, delivery.Payload)
        };

        var (statusCode, body) = await _httpClient.PostAsync(subscription.Url, delivery.Payload, headers);
        var succeeded = statusCode is >= 200 and < 300;

        delivery.Attempts++;
        delivery.LastAttemptUtc = DateTime.UtcNow;
        delivery.ResponseStatusCode = statusCode;
        delivery.ResponseBody = body?.Length > NopwaraWebhooksDefaults.RESPONSE_LOG_LENGTH ? body[..NopwaraWebhooksDefaults.RESPONSE_LOG_LENGTH] : body;

        if (succeeded)
        {
            delivery.Status = WebhookDeliveryStatus.Succeeded;
            delivery.NextAttemptUtc = null;
        }
        else if (delivery.Attempts > NopwaraWebhooksDefaults.RetryDelays.Length)
        {
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.NextAttemptUtc = null;
        }
        else
        {
            delivery.Status = WebhookDeliveryStatus.Pending;
            delivery.NextAttemptUtc = DateTime.UtcNow.Add(NopwaraWebhooksDefaults.RetryDelays[delivery.Attempts - 1]);
        }

        await _deliveryRepository.UpdateAsync(delivery, false);

        return succeeded;
    }

    /// <summary>
    /// Sends the pending deliveries that are due (called by the schedule task) and prunes the log
    /// </summary>
    /// <remarks>
    /// Each delivery is claimed (its next attempt is pushed past the run) before it is sent, so an overlapping run
    /// (slow endpoints, "Send pending now", several web farm nodes) doesn't pick it up again. A run stops after
    /// RUN_TIME_BUDGET so one slow endpoint can't hold the queue for long; the rest is sent by the next run.
    /// </remarks>
    public async Task SendPendingAsync(int batchSize = 50)
    {
        if (!_settings.Enabled)
            return;

        var started = DateTime.UtcNow;
        var activeSubscriptionIds = (await _subscriptionRepository.GetAllAsync(query => query.Where(s => s.IsActive))).Select(s => s.Id).ToList();

        var due = await _deliveryRepository.GetAllAsync(query => query
            .Where(d => d.StatusId == (int)WebhookDeliveryStatus.Pending && d.NextAttemptUtc <= started && activeSubscriptionIds.Contains(d.SubscriptionId))
            .OrderBy(d => d.NextAttemptUtc)
            .Take(batchSize));

        foreach (var candidate in due)
        {
            if (DateTime.UtcNow - started > NopwaraWebhooksDefaults.RUN_TIME_BUDGET)
                break;

            //claim: re-read, skip if another run took it, then lease it for the time a send can take
            var delivery = await _deliveryRepository.GetByIdAsync(candidate.Id, cache => default);
            if (delivery is not { Status: WebhookDeliveryStatus.Pending } || delivery.NextAttemptUtc > DateTime.UtcNow)
                continue;

            delivery.NextAttemptUtc = DateTime.UtcNow.AddSeconds(Math.Clamp(_settings.TimeoutSeconds, 1, 60) * 2 + 30);
            await _deliveryRepository.UpdateAsync(delivery, false);

            await SendAsync(delivery);
        }

        if (_settings.KeepDeliveriesDays > 0)
        {
            var before = DateTime.UtcNow.AddDays(-_settings.KeepDeliveriesDays);
            await _deliveryRepository.DeleteAsync(d => (d.StatusId == (int)WebhookDeliveryStatus.Succeeded || d.StatusId == (int)WebhookDeliveryStatus.Failed) && d.CreatedOnUtc < before);
        }
    }

    /// <summary>
    /// Sends a delivery again now (failed or succeeded), starting a new retry cycle
    /// </summary>
    public async Task<bool> RedeliverAsync(WebhookDelivery delivery)
    {
        delivery.Attempts = 0;
        delivery.Status = WebhookDeliveryStatus.Pending;
        delivery.NextAttemptUtc = DateTime.UtcNow;

        return await SendAsync(delivery);
    }

    #endregion
}
