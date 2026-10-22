using Nop.Core;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Domain;

/// <summary>
/// Represents an endpoint that receives webhook events
/// </summary>
public class WebhookSubscription : BaseEntity
{
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL (POST, JSON body)
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the secret used to sign the payload (HMAC-SHA256, X-Nopwara-Signature header)
    /// </summary>
    public string Secret { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated event names the endpoint receives ("*" = all)
    /// </summary>
    public string Events { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the store identifier (0 = all stores)
    /// </summary>
    public int StoreId { get; set; }

    public DateTime CreatedOnUtc { get; set; }
}
