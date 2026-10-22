using Nop.Core.Domain.ScheduleTasks;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.NopwaraWebhooks;

/// <summary>
/// Represents the Nopwara webhooks plugin
/// </summary>
public class NopwaraWebhooksPlugin : BasePlugin, IMiscPlugin
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly INopUrlHelper _nopUrlHelper;
    private readonly IScheduleTaskService _scheduleTaskService;
    private readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public NopwaraWebhooksPlugin(ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IScheduleTaskService scheduleTaskService,
        ISettingService settingService)
    {
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _scheduleTaskService = scheduleTaskService;
        _settingService = settingService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return _nopUrlHelper.RouteUrl(NopwaraWebhooksDefaults.CONFIGURATION_ROUTE_NAME);
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new NopwaraWebhooksSettings { Enabled = true, TimeoutSeconds = 10, KeepDeliveriesDays = 30 });

        if (await _scheduleTaskService.GetTaskByTypeAsync(NopwaraWebhooksDefaults.DeliveryTask.Type) is null)
        {
            await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
            {
                Enabled = true,
                LastEnabledUtc = DateTime.UtcNow,
                StopOnError = false,
                Name = NopwaraWebhooksDefaults.DeliveryTask.Name,
                Type = NopwaraWebhooksDefaults.DeliveryTask.Type,
                Seconds = NopwaraWebhooksDefaults.DeliveryTask.Period
            });
        }

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.NopwaraWebhooks.Webhooks"] = "Webhooks",
            ["Plugins.Misc.NopwaraWebhooks.Instructions"] = "Each endpoint receives a JSON POST per event, signed with its secret: X-Nopwara-Signature = \"sha256=\" + HMAC-SHA256(secret, raw body) in hex, plus X-Nopwara-Event, X-Nopwara-Delivery (unique id, use it to ignore duplicates) and X-Nopwara-Timestamp. Any 2xx response is a success; otherwise the delivery is retried after 1 min, 5 min, 30 min, 2 h and 12 h. Deliveries are sent by the \"Nopwara webhooks: send pending deliveries\" schedule task (every minute).",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Enabled"] = "Enabled",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Enabled.Hint"] = "Queue and send webhook events.",
            ["Plugins.Misc.NopwaraWebhooks.Fields.TimeoutSeconds"] = "Timeout (seconds)",
            ["Plugins.Misc.NopwaraWebhooks.Fields.TimeoutSeconds.Hint"] = "How long to wait for an endpoint to answer.",
            ["Plugins.Misc.NopwaraWebhooks.Fields.KeepDeliveriesDays"] = "Keep log (days)",
            ["Plugins.Misc.NopwaraWebhooks.Fields.KeepDeliveriesDays.Hint"] = "Succeeded deliveries older than this are deleted (0 = keep).",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Name"] = "Name",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Name.Hint"] = "A name for this endpoint, e.g. \"ERP\" or \"Zapier orders\".",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Url"] = "Endpoint URL",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Url.Invalid"] = "Enter an absolute http:// or https:// URL.",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Url.Hint"] = "The URL the events are POSTed to (https recommended).",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Secret"] = "Signing secret",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Secret.Hint"] = "Used to sign every request; keep it private.",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Secret.Info"] = "Verify requests by computing HMAC-SHA256 of the raw body with this secret and comparing it to the X-Nopwara-Signature header.",
            ["Plugins.Misc.NopwaraWebhooks.Fields.AllEvents"] = "All events",
            ["Plugins.Misc.NopwaraWebhooks.Fields.AllEvents.Hint"] = "Receive every event, including ones added in future versions.",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Events"] = "Events",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Events.Hint"] = "The events this endpoint receives.",
            ["Plugins.Misc.NopwaraWebhooks.Fields.IsActive"] = "Active",
            ["Plugins.Misc.NopwaraWebhooks.Fields.IsActive.Hint"] = "Inactive endpoints receive nothing.",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Store"] = "Store",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Store.Hint"] = "Only receive events of this store (events without a store are always sent).",
            ["Plugins.Misc.NopwaraWebhooks.Fields.CreatedOn"] = "Created on",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Event"] = "Event",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Status"] = "Status",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Attempts"] = "Attempts",
            ["Plugins.Misc.NopwaraWebhooks.Fields.Response"] = "Last response",
            ["Plugins.Misc.NopwaraWebhooks.Status.Pending"] = "Pending",
            ["Plugins.Misc.NopwaraWebhooks.Status.Succeeded"] = "Succeeded",
            ["Plugins.Misc.NopwaraWebhooks.Status.Failed"] = "Failed",
            ["Plugins.Misc.NopwaraWebhooks.AddNew"] = "Add a new webhook endpoint",
            ["Plugins.Misc.NopwaraWebhooks.Edit"] = "Edit webhook endpoint",
            ["Plugins.Misc.NopwaraWebhooks.BackToList"] = "back to webhook list",
            ["Plugins.Misc.NopwaraWebhooks.Added"] = "The endpoint has been added. Use \"Send test\" to check it.",
            ["Plugins.Misc.NopwaraWebhooks.Updated"] = "The endpoint has been updated.",
            ["Plugins.Misc.NopwaraWebhooks.Test"] = "Send test",
            ["Plugins.Misc.NopwaraWebhooks.Test.Succeeded"] = "The test event was delivered (HTTP {0}).",
            ["Plugins.Misc.NopwaraWebhooks.Test.Failed"] = "The test event could not be delivered: {0}. It will be retried.",
            ["Plugins.Misc.NopwaraWebhooks.Deliveries"] = "Deliveries",
            ["Plugins.Misc.NopwaraWebhooks.Payload"] = "Payload",
            ["Plugins.Misc.NopwaraWebhooks.Redeliver"] = "Redeliver",
            ["Plugins.Misc.NopwaraWebhooks.SendPending"] = "Send pending now",
            ["Plugins.Misc.NopwaraWebhooks.PendingSent"] = "The pending deliveries that were due have been sent."
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    public override async Task UninstallAsync()
    {
        var task = await _scheduleTaskService.GetTaskByTypeAsync(NopwaraWebhooksDefaults.DeliveryTask.Type);
        if (task != null)
            await _scheduleTaskService.DeleteTaskAsync(task);

        await _settingService.DeleteSettingAsync<NopwaraWebhooksSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.NopwaraWebhooks");

        await base.UninstallAsync();
    }

    #endregion
}
