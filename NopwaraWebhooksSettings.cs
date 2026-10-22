using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.NopwaraWebhooks;

/// <summary>
/// Represents the plugin settings
/// </summary>
public class NopwaraWebhooksSettings : ISettings
{
    /// <summary>
    /// Gets or sets a value indicating whether events are queued and sent
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the number of days succeeded deliveries are kept in the log
    /// </summary>
    public int KeepDeliveriesDays { get; set; }
}
