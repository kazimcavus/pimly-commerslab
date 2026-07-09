using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Channels.Application.Connections;
using Channels.Domain.Connections;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Channels.Infrastructure.Trendyol;

/// <summary>
/// Trendyol API çağrıları için ortak HTTP desteği: Basic auth başlıkları,
/// 429/5xx'te Retry-After duyarlı üstel backoff ve JSON çözümleme.
/// </summary>
internal static class TrendyolHttpSupport
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(500);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Pazaryeri için etkin herhangi bir bağlantının kimlik bilgilerini çözer.
    /// Taksonomi/attribute uçları pazaryeri-global olduğundan tenant seçimi önemsizdir; bağlantı yoksa null döner.
    /// </summary>
    public static async Task<MarketplaceCredentials?> ResolveAnyEnabledCredentialsAsync(
        IMarketplaceConnectionRepository connections,
        Marketplace marketplace,
        CancellationToken cancellationToken)
    {
        var connection = await connections.GetAnyEnabledAsync(marketplace, cancellationToken);
        return connection is null
            ? null
            : new MarketplaceCredentials(connection.SellerId, connection.ApiKey, connection.ApiSecret);
    }

    /// <summary>İstek için Basic auth ve User-Agent başlıklarını hazırlar.</summary>
    public static void ApplyHeaders(HttpRequestMessage request, MarketplaceCredentials? credentials)
    {
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.ApiKey))
        {
            return;
        }

        var token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{credentials.ApiKey}:{credentials.ApiSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);

        var agent = string.IsNullOrWhiteSpace(credentials.SellerId)
            ? "pimly - SelfIntegration"
            : $"{credentials.SellerId} - SelfIntegration";
        request.Headers.TryAddWithoutValidation("User-Agent", agent);
    }

    /// <summary>GET isteği yapar, geçici hatalarda backoff ile tekrar dener ve JSON gövdeyi çözer.</summary>
    public static async Task<Result<T>> GetJsonAsync<T>(
        HttpClient httpClient,
        string requestUri,
        MarketplaceCredentials? credentials,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            ApplyHeaders(request, credentials);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(ex, "Trendyol request failed (attempt {Attempt}/{Max}): {Uri}", attempt, MaxAttempts, requestUri);
                await Task.Delay(ComputeDelay(attempt, null), cancellationToken);
                continue;
            }
            catch (HttpRequestException ex)
            {
                return Result.Failure<T>(Error.Failure($"Trendyol request failed: {ex.Message}"));
            }

            using (response)
            {
                if (IsTransient(response.StatusCode) && attempt < MaxAttempts)
                {
                    var delay = ComputeDelay(attempt, response.Headers.RetryAfter?.Delta);
                    logger.LogWarning(
                        "Trendyol responded {Status}; retrying in {Delay} (attempt {Attempt}/{Max}): {Uri}",
                        (int)response.StatusCode,
                        delay,
                        attempt,
                        MaxAttempts,
                        requestUri);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    var snippet = body.Length > 300 ? body[..300] : body;
                    return Result.Failure<T>(Error.Failure(
                        $"Trendyol responded {(int)response.StatusCode} for {requestUri}: {snippet}"));
                }

                try
                {
                    var payload = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var parsed = await JsonSerializer.DeserializeAsync<T>(payload, JsonOptions, cancellationToken);
                    if (parsed is null)
                    {
                        return Result.Failure<T>(Error.Failure("Trendyol response body was empty."));
                    }

                    return Result.Success(parsed);
                }
                catch (JsonException ex)
                {
                    return Result.Failure<T>(Error.Failure($"Trendyol response could not be parsed: {ex.Message}"));
                }
            }
        }

        return Result.Failure<T>(Error.Failure("Trendyol request exhausted retry attempts."));
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    private static TimeSpan ComputeDelay(int attempt, TimeSpan? retryAfter)
    {
        if (retryAfter is { } fromHeader && fromHeader > TimeSpan.Zero)
        {
            return fromHeader;
        }

        return TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
    }
}
