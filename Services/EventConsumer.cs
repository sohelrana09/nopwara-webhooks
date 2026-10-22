using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Services.Catalog;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Services;

/// <summary>
/// Turns nopCommerce events into webhook deliveries and adds the admin menu item
/// </summary>
public class EventConsumer :
    IConsumer<OrderPlacedEvent>,
    IConsumer<OrderPaidEvent>,
    IConsumer<OrderStatusChangedEvent>,
    IConsumer<OrderRefundedEvent>,
    IConsumer<CustomerRegisteredEvent>,
    IConsumer<EntityUpdatedEvent<Product>>,
    IConsumer<EntityInsertedEvent<StockQuantityHistory>>,
    IConsumer<ShipmentSentEvent>,
    IConsumer<AdminMenuCreatedEvent>
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly IOrderService _orderService;
    private readonly IPluginManager<IPlugin> _pluginManager;
    private readonly IProductService _productService;
    private readonly WebhookService _webhookService;

    #endregion

    #region Ctor

    public EventConsumer(ILocalizationService localizationService,
        IOrderService orderService,
        IPluginManager<IPlugin> pluginManager,
        IProductService productService,
        WebhookService webhookService)
    {
        _localizationService = localizationService;
        _orderService = orderService;
        _pluginManager = pluginManager;
        _productService = productService;
        _webhookService = webhookService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// The consumer is discovered as soon as the assembly is loaded, so check the plugin is installed
    /// </summary>
    private async Task<bool> IsInstalledAsync()
    {
        return await _pluginManager.LoadPluginBySystemNameAsync(NopwaraWebhooksDefaults.SYSTEM_NAME) != null;
    }

    private async Task<object> OrderDataAsync(Order order)
    {
        var items = new List<object>();
        foreach (var item in await _orderService.GetOrderItemsAsync(order.Id))
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            items.Add(new
            {
                productId = item.ProductId,
                sku = product?.Sku,
                name = product?.Name,
                attributes = item.AttributeDescription,
                quantity = item.Quantity,
                unitPriceInclTax = item.UnitPriceInclTax,
                unitPriceExclTax = item.UnitPriceExclTax,
                priceInclTax = item.PriceInclTax,
                priceExclTax = item.PriceExclTax
            });
        }

        return new
        {
            id = order.Id,
            orderGuid = order.OrderGuid,
            customOrderNumber = order.CustomOrderNumber,
            customerId = order.CustomerId,
            orderStatus = order.OrderStatus.ToString(),
            paymentStatus = order.PaymentStatus.ToString(),
            shippingStatus = order.ShippingStatus.ToString(),
            paymentMethod = order.PaymentMethodSystemName,
            shippingMethod = order.ShippingMethod,
            currency = order.CustomerCurrencyCode,
            orderSubtotalInclTax = order.OrderSubtotalInclTax,
            orderShippingInclTax = order.OrderShippingInclTax,
            orderTax = order.OrderTax,
            orderTotal = order.OrderTotal,
            createdOnUtc = order.CreatedOnUtc,
            items
        };
    }

    private static object ProductData(Product product)
    {
        return new
        {
            id = product.Id,
            name = product.Name,
            sku = product.Sku,
            price = product.Price,
            oldPrice = product.OldPrice,
            stockQuantity = product.StockQuantity,
            published = product.Published,
            deleted = product.Deleted,
            updatedOnUtc = product.UpdatedOnUtc
        };
    }

    #endregion

    #region Methods

    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        if (await IsInstalledAsync())
            await _webhookService.QueueAsync(WebhookEvents.ORDER_PLACED, await OrderDataAsync(eventMessage.Order), eventMessage.Order.StoreId);
    }

    public async Task HandleEventAsync(OrderPaidEvent eventMessage)
    {
        if (await IsInstalledAsync())
            await _webhookService.QueueAsync(WebhookEvents.ORDER_PAID, await OrderDataAsync(eventMessage.Order), eventMessage.Order.StoreId);
    }

    public async Task HandleEventAsync(OrderStatusChangedEvent eventMessage)
    {
        if (!await IsInstalledAsync())
            return;

        var data = new { previousOrderStatus = eventMessage.PreviousOrderStatus.ToString(), order = await OrderDataAsync(eventMessage.Order) };
        await _webhookService.QueueAsync(WebhookEvents.ORDER_STATUS_CHANGED, data, eventMessage.Order.StoreId);
    }

    public async Task HandleEventAsync(OrderRefundedEvent eventMessage)
    {
        if (!await IsInstalledAsync())
            return;

        var data = new { amount = eventMessage.Amount, order = await OrderDataAsync(eventMessage.Order) };
        await _webhookService.QueueAsync(WebhookEvents.ORDER_REFUNDED, data, eventMessage.Order.StoreId);
    }

    public async Task HandleEventAsync(CustomerRegisteredEvent eventMessage)
    {
        if (!await IsInstalledAsync())
            return;

        var customer = eventMessage.Customer;
        var data = new
        {
            id = customer.Id,
            customerGuid = customer.CustomerGuid,
            email = customer.Email,
            username = customer.Username,
            firstName = customer.FirstName,
            lastName = customer.LastName,
            createdOnUtc = customer.CreatedOnUtc
        };
        await _webhookService.QueueAsync(WebhookEvents.CUSTOMER_REGISTERED, data, customer.RegisteredInStoreId);
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<Product> eventMessage)
    {
        if (await IsInstalledAsync())
            await _webhookService.QueueAsync(WebhookEvents.PRODUCT_UPDATED, ProductData(eventMessage.Entity), dedupeKey: eventMessage.Entity.Id.ToString());
    }

    public async Task HandleEventAsync(EntityInsertedEvent<StockQuantityHistory> eventMessage)
    {
        if (!await IsInstalledAsync())
            return;

        var history = eventMessage.Entity;
        var data = new
        {
            productId = history.ProductId,
            combinationId = history.CombinationId,
            warehouseId = history.WarehouseId,
            quantityAdjustment = history.QuantityAdjustment,
            stockQuantity = history.StockQuantity,
            message = history.Message,
            createdOnUtc = history.CreatedOnUtc
        };
        await _webhookService.QueueAsync(WebhookEvents.PRODUCT_STOCK_CHANGED, data);
    }

    public async Task HandleEventAsync(ShipmentSentEvent eventMessage)
    {
        if (!await IsInstalledAsync())
            return;

        var shipment = eventMessage.Shipment;
        var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
        var data = new
        {
            id = shipment.Id,
            orderId = shipment.OrderId,
            customOrderNumber = order?.CustomOrderNumber,
            trackingNumber = shipment.TrackingNumber,
            shippedDateUtc = shipment.ShippedDateUtc,
            createdOnUtc = shipment.CreatedOnUtc
        };
        await _webhookService.QueueAsync(WebhookEvents.SHIPMENT_SENT, data, order?.StoreId ?? 0);
    }

    public async Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
    {
        if (!await IsInstalledAsync())
            return;

        var configuration = eventMessage.RootMenuItem.GetItemBySystemName("Configuration");
        configuration?.InsertAfter("Widgets", new AdminMenuItem
        {
            Visible = true,
            SystemName = NopwaraWebhooksDefaults.ADMIN_MENU_SYSTEM_NAME,
            Title = await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraWebhooks.Webhooks"),
            Url = eventMessage.GetMenuItemUrl("NopwaraWebhooksAdmin", "List"),
            IconClass = "far fa-dot-circle",
            PermissionNames = new List<string> { StandardPermission.Configuration.MANAGE_PLUGINS }
        });
    }

    #endregion
}
