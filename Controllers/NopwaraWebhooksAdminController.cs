using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Misc.NopwaraWebhooks.Domain;
using Nop.Plugin.Misc.NopwaraWebhooks.Models;
using Nop.Plugin.Misc.NopwaraWebhooks.Services;
using Nop.Services.Configuration;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Controllers;

[Area(AreaNames.ADMIN)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
public class NopwaraWebhooksAdminController : BasePluginController
{
    #region Fields

    private const string VIEWS = $"~/Plugins/{NopwaraWebhooksDefaults.PLUGIN_FOLDER}/Views/Admin/";

    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly ISettingService _settingService;
    private readonly IStoreService _storeService;
    private readonly NopwaraWebhooksSettings _settings;
    private readonly WebhookService _webhookService;

    #endregion

    #region Ctor

    public NopwaraWebhooksAdminController(IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        INotificationService notificationService,
        ISettingService settingService,
        IStoreService storeService,
        NopwaraWebhooksSettings settings,
        WebhookService webhookService)
    {
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _settingService = settingService;
        _storeService = storeService;
        _settings = settings;
        _webhookService = webhookService;
    }

    #endregion

    #region Utilities

    private async Task<SubscriptionModel> PrepareModelAsync(SubscriptionModel model, WebhookSubscription subscription)
    {
        if (subscription != null)
        {
            var events = subscription.Events.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            model.Id = subscription.Id;
            model.Name = subscription.Name;
            model.Url = subscription.Url;
            model.Secret = subscription.Secret;
            model.IsActive = subscription.IsActive;
            model.StoreId = subscription.StoreId;
            model.AllEvents = events.Contains(WebhookEvents.ALL);
            model.SelectedEvents = events.Where(e => e != WebhookEvents.ALL).ToList();
            model.DeliverySearchModel.SubscriptionId = subscription.Id;
            model.DeliverySearchModel.SetGridPageSize();
        }

        model.AvailableEvents = WebhookEvents.Available
            .Select(e => new SelectListItem(e, e, model.SelectedEvents.Contains(e)))
            .ToList();

        model.AvailableStores.Clear();
        model.AvailableStores.Add(new SelectListItem(await _localizationService.GetResourceAsync("Admin.Common.All"), "0"));
        foreach (var store in await _storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem(store.Name, store.Id.ToString()));

        return model;
    }

    /// <summary>
    /// Accepts absolute http(s) URLs, including localhost / private addresses (jQuery Validate's "url" rule rejects those)
    /// </summary>
    private async Task ValidateUrlAsync(SubscriptionModel model)
    {
        if (!Uri.TryCreate(model.Url?.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            ModelState.AddModelError(nameof(model.Url), await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraWebhooks.Fields.Url.Invalid"));
    }

    private static string EventsOf(SubscriptionModel model)
    {
        return model.AllEvents
            ? WebhookEvents.ALL
            : string.Join(",", (model.SelectedEvents ?? new List<string>()).Where(WebhookEvents.Available.Contains));
    }

    #endregion

    #region Subscriptions

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public IActionResult List()
    {
        var model = new SubscriptionSearchModel();
        model.SetGridPageSize();

        return View(VIEWS + "List.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> SubscriptionList(SubscriptionSearchModel searchModel)
    {
        var subscriptions = await _webhookService.SearchSubscriptionsAsync(searchModel.Page - 1, searchModel.PageSize);
        var model = new SubscriptionListModel().PrepareToGrid(searchModel, subscriptions, () => subscriptions.Select(s => new SubscriptionModel
        {
            Id = s.Id,
            Name = s.Name,
            Url = s.Url,
            IsActive = s.IsActive,
            EventsText = s.Events == WebhookEvents.ALL ? "*" : s.Events.Replace(",", ", ")
        }));

        return Json(model);
    }

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Create()
    {
        var model = await PrepareModelAsync(new SubscriptionModel { IsActive = true, Secret = WebhookService.GenerateSecret(), AllEvents = true }, null);

        return View(VIEWS + "Edit.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Create(SubscriptionModel model)
    {
        await ValidateUrlAsync(model);
        if (!ModelState.IsValid)
            return View(VIEWS + "Edit.cshtml", await PrepareModelAsync(model, null));

        var subscription = new WebhookSubscription
        {
            Name = model.Name.Trim(),
            Url = model.Url.Trim(),
            Secret = string.IsNullOrWhiteSpace(model.Secret) ? WebhookService.GenerateSecret() : model.Secret.Trim(),
            Events = EventsOf(model),
            IsActive = model.IsActive,
            StoreId = model.StoreId,
            CreatedOnUtc = DateTime.UtcNow
        };
        await _webhookService.InsertSubscriptionAsync(subscription);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraWebhooks.Added"));

        return RedirectToAction("Edit", new { id = subscription.Id });
    }

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Edit(int id)
    {
        var subscription = await _webhookService.GetSubscriptionByIdAsync(id);
        if (subscription == null)
            return RedirectToAction("List");

        return View(VIEWS + "Edit.cshtml", await PrepareModelAsync(new SubscriptionModel(), subscription));
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Edit(SubscriptionModel model)
    {
        var subscription = await _webhookService.GetSubscriptionByIdAsync(model.Id);
        if (subscription == null)
            return RedirectToAction("List");

        await ValidateUrlAsync(model);
        if (!ModelState.IsValid)
            return View(VIEWS + "Edit.cshtml", await PrepareModelAsync(model, subscription));

        subscription.Name = model.Name.Trim();
        subscription.Url = model.Url.Trim();
        subscription.Secret = string.IsNullOrWhiteSpace(model.Secret) ? subscription.Secret : model.Secret.Trim();
        subscription.Events = EventsOf(model);
        subscription.IsActive = model.IsActive;
        subscription.StoreId = model.StoreId;
        await _webhookService.UpdateSubscriptionAsync(subscription);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraWebhooks.Updated"));

        return RedirectToAction("Edit", new { id = subscription.Id });
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Delete(int id)
    {
        var subscription = await _webhookService.GetSubscriptionByIdAsync(id);
        if (subscription != null)
            await _webhookService.DeleteSubscriptionAsync(subscription);

        return RedirectToAction("List");
    }

    /// <summary>
    /// Sends a "ping" event to the endpoint right away
    /// </summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> SendTest(int id)
    {
        var subscription = await _webhookService.GetSubscriptionByIdAsync(id);
        if (subscription == null)
            return RedirectToAction("List");

        var delivery = await _webhookService.CreateDeliveryAsync(subscription, WebhookEvents.PING,
            new { message = "Hello from nopCommerce (Nopwara webhooks)", subscriptionId = subscription.Id }, subscription.StoreId);

        if (await _webhookService.SendAsync(delivery))
            _notificationService.SuccessNotification(string.Format(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraWebhooks.Test.Succeeded"), delivery.ResponseStatusCode));
        else
            _notificationService.ErrorNotification(string.Format(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraWebhooks.Test.Failed"), delivery.ResponseStatusCode?.ToString() ?? delivery.ResponseBody));

        return RedirectToAction("Edit", new { id = subscription.Id });
    }

    #endregion

    #region Deliveries

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> DeliveryList(DeliverySearchModel searchModel)
    {
        var deliveries = await _webhookService.SearchDeliveriesAsync(searchModel.SubscriptionId, pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);
        var model = await new DeliveryListModel().PrepareToGridAsync(searchModel, deliveries, () => deliveries.SelectAwait(async d => new DeliveryModel
        {
            Id = d.Id,
            Event = d.Event,
            Status = await _localizationService.GetResourceAsync($"Plugins.Misc.NopwaraWebhooks.Status.{d.Status}"),
            Attempts = d.Attempts,
            Response = d.ResponseStatusCode.HasValue ? $"{d.ResponseStatusCode} {d.ResponseBody}" : d.ResponseBody,
            CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(d.CreatedOnUtc, DateTimeKind.Utc),
            NextAttempt = d.NextAttemptUtc.HasValue ? await _dateTimeHelper.ConvertToUserTimeAsync(d.NextAttemptUtc.Value, DateTimeKind.Utc) : null,
            Payload = d.Payload
        }));

        return Json(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Redeliver(int id)
    {
        var delivery = await _webhookService.GetDeliveryByIdAsync(id);
        if (delivery == null)
            return Json(new { success = false });

        var success = await _webhookService.RedeliverAsync(delivery);

        return Json(new { success, status = delivery.ResponseStatusCode });
    }

    /// <summary>
    /// Sends the due pending deliveries now (same as the schedule task)
    /// </summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> SendPending()
    {
        await _webhookService.SendPendingAsync();
        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraWebhooks.PendingSent"));

        return RedirectToAction("List");
    }

    #endregion

    #region Configuration

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public IActionResult Configure()
    {
        var model = new ConfigurationModel
        {
            Enabled = _settings.Enabled,
            TimeoutSeconds = _settings.TimeoutSeconds,
            KeepDeliveriesDays = _settings.KeepDeliveriesDays
        };

        return View(VIEWS + "Configure.cshtml", model);
    }

    [HttpPost, ActionName("Configure")]
    [FormValueRequired("save")]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return Configure();

        _settings.Enabled = model.Enabled;
        _settings.TimeoutSeconds = Math.Clamp(model.TimeoutSeconds, 1, 60);
        _settings.KeepDeliveriesDays = Math.Max(0, model.KeepDeliveriesDays);
        await _settingService.SaveSettingAsync(_settings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return Configure();
    }

    #endregion
}
