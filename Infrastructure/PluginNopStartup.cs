using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Infrastructure.Extensions;
using Nop.Plugin.Misc.NopwaraWebhooks.Services;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Infrastructure;

/// <summary>
/// Represents object for the configuring services on application startup
/// </summary>
public class PluginNopStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var webhookClient = services.AddHttpClient<WebhookHttpClient>();
        webhookClient.WithProxy();
        //never follow redirects: a 3xx is a failed delivery, and a redirect can't send the request (or its response) to another host
        webhookClient.ConfigurePrimaryHttpMessageHandler((handler, _) =>
        {
            if (handler is HttpClientHandler httpClientHandler)
                httpClientHandler.AllowAutoRedirect = false;
        });
        services.AddScoped<WebhookService>();
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => 3000;
}
