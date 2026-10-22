namespace Nop.Plugin.Misc.NopwaraWebhooks;

/// <summary>
/// Represents plugin constants
/// </summary>
public static class NopwaraWebhooksDefaults
{
    public const string SYSTEM_NAME = "Misc.NopwaraWebhooks";

    public const string PLUGIN_FOLDER = "Misc.NopwaraWebhooks";

    public const string CONFIGURATION_ROUTE_NAME = "Plugin.Misc.NopwaraWebhooks.Configure";

    public const string ADMIN_MENU_SYSTEM_NAME = "Nopwara webhooks";

    /// <summary>
    /// Gets the delivery schedule task (name, type, period in seconds)
    /// </summary>
    public static (string Name, string Type, int Period) DeliveryTask => ("Nopwara webhooks: send pending deliveries", "Nop.Plugin.Misc.NopwaraWebhooks.Services.WebhookDeliveryTask", 60);

    /// <summary>
    /// Gets the delays before each retry; a delivery fails after the last one
    /// </summary>
    public static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), TimeSpan.FromHours(12)];

    /// <summary>
    /// Gets how long one run of the delivery task may keep sending (the task runs every minute)
    /// </summary>
    public static readonly TimeSpan RUN_TIME_BUDGET = TimeSpan.FromSeconds(50);

    /// <summary>
    /// Gets the number of characters of the response body kept in the delivery log
    /// </summary>
    public const int RESPONSE_LOG_LENGTH = 1000;
}

/// <summary>
/// Represents the event names a subscription can receive
/// </summary>
public static class WebhookEvents
{
    public const string ALL = "*";
    public const string PING = "ping";
    public const string ORDER_PLACED = "order.placed";
    public const string ORDER_PAID = "order.paid";
    public const string ORDER_STATUS_CHANGED = "order.status_changed";
    public const string ORDER_REFUNDED = "order.refunded";
    public const string CUSTOMER_REGISTERED = "customer.registered";
    public const string PRODUCT_UPDATED = "product.updated";
    public const string PRODUCT_STOCK_CHANGED = "product.stock_changed";
    public const string SHIPMENT_SENT = "shipment.sent";

    /// <summary>
    /// Gets the events shown in admin
    /// </summary>
    public static readonly string[] Available = [ORDER_PLACED, ORDER_PAID, ORDER_STATUS_CHANGED, ORDER_REFUNDED, CUSTOMER_REGISTERED, PRODUCT_UPDATED, PRODUCT_STOCK_CHANGED, SHIPMENT_SENT];
}
