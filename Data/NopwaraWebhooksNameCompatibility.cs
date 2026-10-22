using Nop.Data.Mapping;
using Nop.Plugin.Misc.NopwaraWebhooks.Domain;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Data;

/// <summary>
/// Plugin table naming compatibility
/// </summary>
public class NopwaraWebhooksNameCompatibility : INameCompatibility
{
    public Dictionary<Type, string> TableNames => new()
    {
        [typeof(WebhookSubscription)] = "NopwaraWebhookSubscription",
        [typeof(WebhookDelivery)] = "NopwaraWebhookDelivery"
    };

    public Dictionary<(Type, string), string> ColumnName => new();
}
