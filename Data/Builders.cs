using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.NopwaraWebhooks.Domain;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Data;

public class WebhookSubscriptionBuilder : NopEntityBuilder<WebhookSubscription>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(WebhookSubscription.Name)).AsString(400).NotNullable()
            .WithColumn(nameof(WebhookSubscription.Url)).AsString(2000).NotNullable()
            .WithColumn(nameof(WebhookSubscription.Secret)).AsString(200).NotNullable()
            .WithColumn(nameof(WebhookSubscription.Events)).AsString(2000).NotNullable();
    }
}

public class WebhookDeliveryBuilder : NopEntityBuilder<WebhookDelivery>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(WebhookDelivery.SubscriptionId)).AsInt32().ForeignKey<WebhookSubscription>()
            .WithColumn(nameof(WebhookDelivery.Event)).AsString(100).NotNullable()
            .WithColumn(nameof(WebhookDelivery.Payload)).AsString(int.MaxValue).NotNullable()
            .WithColumn(nameof(WebhookDelivery.ResponseBody)).AsString(2000).Nullable();
    }
}
