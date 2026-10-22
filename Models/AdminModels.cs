using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Models;

public record SubscriptionSearchModel : BaseSearchModel;

public record SubscriptionListModel : BasePagedListModel<SubscriptionModel>;

/// <summary>
/// Represents a webhook subscription (grid row and create / edit page)
/// </summary>
public record SubscriptionModel : BaseNopEntityModel
{
    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.Name")]
    [Required]
    public string Name { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.Url")]
    [Required]
    public string Url { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.Secret")]
    public string Secret { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.Events")]
    public IList<string> SelectedEvents { get; set; } = new List<string>();

    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.AllEvents")]
    public bool AllEvents { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.IsActive")]
    public bool IsActive { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.Store")]
    public int StoreId { get; set; }

    public string EventsText { get; set; }

    public IList<SelectListItem> AvailableEvents { get; set; } = new List<SelectListItem>();

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();

    public DeliverySearchModel DeliverySearchModel { get; set; } = new();
}

public record DeliverySearchModel : BaseSearchModel
{
    public int SubscriptionId { get; set; }
}

public record DeliveryListModel : BasePagedListModel<DeliveryModel>;

/// <summary>
/// Represents a delivery log row
/// </summary>
public record DeliveryModel : BaseNopEntityModel
{
    public string Event { get; set; }

    public string Status { get; set; }

    public int Attempts { get; set; }

    public string Response { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? NextAttempt { get; set; }

    public string Payload { get; set; }
}

/// <summary>
/// Represents the plugin configuration model
/// </summary>
public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.Enabled")]
    public bool Enabled { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.TimeoutSeconds")]
    public int TimeoutSeconds { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraWebhooks.Fields.KeepDeliveriesDays")]
    public int KeepDeliveriesDays { get; set; }
}
