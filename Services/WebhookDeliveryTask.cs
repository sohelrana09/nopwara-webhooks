using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Services;

/// <summary>
/// Sends the pending webhook deliveries (Nopwara webhooks: send pending deliveries, every minute)
/// </summary>
public class WebhookDeliveryTask : IScheduleTask
{
    private readonly WebhookService _webhookService;

    public WebhookDeliveryTask(WebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    public async Task ExecuteAsync()
    {
        await _webhookService.SendPendingAsync();
    }
}
