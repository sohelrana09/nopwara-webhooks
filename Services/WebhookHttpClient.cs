using System.Net.Http.Headers;
using System.Text;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Services;

/// <summary>
/// Sends webhook requests
/// </summary>
public class WebhookHttpClient
{
    private readonly HttpClient _httpClient;

    public WebhookHttpClient(HttpClient httpClient, NopwaraWebhooksSettings settings)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 1, 60));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Nopwara-Webhooks", "1.0"));
    }

    /// <summary>
    /// Posts the JSON body with the event headers
    /// </summary>
    /// <returns>Status code (null when no response) and the response body or error</returns>
    public async Task<(int? statusCode, string body)> PostAsync(string url, string json, IDictionary<string, string> headers)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        foreach (var header in headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        try
        {
            using var response = await _httpClient.SendAsync(request);
            return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
        catch (Exception exception)
        {
            return (null, exception.Message);
        }
    }
}
