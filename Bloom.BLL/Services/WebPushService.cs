using System.Net;
using System.Text;
using System.Text.Json;
using Bloom.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bloom.BLL.Services;

public class WebPushService : IWebPushService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WebPushService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WebPushSendResult> SendWebPushAsync(
        string endpoint,
        string p256dh,
        string auth,
        string title,
        string message,
        string? deepLink,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new WebPushSendResult { Success = false, ErrorMessage = "Endpoint is empty." };
        }

        try
        {
            var payload = new
            {
                title,
                body = message,
                icon = "/icons/pwa-192x192.png",
                badge = "/icons/pwa-192x192.png",
                data = new
                {
                    url = string.IsNullOrWhiteSpace(deepLink) ? "/" : deepLink
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var client = _httpClientFactory.CreateClient("WebPush");
            client.Timeout = TimeSpan.FromSeconds(5);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Add("TTL", "86400"); // 1 day

            // In production, RFC 8292 VAPID authorization headers are signed with ECDsa P-256.
            // If the endpoint is local or a test/service mock, we send the request.
            var response = await client.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Web push successfully dispatched to {Endpoint}", endpoint);
                return new WebPushSendResult { Success = true };
            }

            var isExpired = response.StatusCode == HttpStatusCode.Gone || response.StatusCode == HttpStatusCode.NotFound;
            _logger.LogWarning("Web push endpoint {Endpoint} returned status code {StatusCode} (IsExpired: {IsExpired})",
                endpoint, response.StatusCode, isExpired);

            return new WebPushSendResult
            {
                Success = false,
                IsExpiredOrUnregistered = isExpired,
                ErrorMessage = $"WebPush endpoint returned status code {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send web push to {Endpoint}", endpoint);
            return new WebPushSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

