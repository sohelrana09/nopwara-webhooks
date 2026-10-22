using Nop.Core;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Domain;

/// <summary>
/// Represents one event sent (or to be sent) to one subscription
/// </summary>
public class WebhookDelivery : BaseEntity
{
    /// <summary>
    /// Gets or sets the delivery identifier sent in the X-Nopwara-Delivery header (receivers use it to ignore duplicates)
    /// </summary>
    public Guid DeliveryGuid { get; set; }

    public int SubscriptionId { get; set; }

    public string Event { get; set; }

    /// <summary>
    /// Gets or sets the JSON body
    /// </summary>
    public string Payload { get; set; }

    public int StatusId { get; set; }

    public WebhookDeliveryStatus Status
    {
        get => (WebhookDeliveryStatus)StatusId;
        set => StatusId = (int)value;
    }

    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets when the next attempt is due (pending deliveries)
    /// </summary>
    public DateTime? NextAttemptUtc { get; set; }

    public DateTime? LastAttemptUtc { get; set; }

    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Gets or sets the beginning of the response body or the error message of the last attempt
    /// </summary>
    public string ResponseBody { get; set; }

    public DateTime CreatedOnUtc { get; set; }
}

/// <summary>
/// Represents a delivery status
/// </summary>
public enum WebhookDeliveryStatus
{
    Pending = 0,
    Succeeded = 10,
    Failed = 20
}
