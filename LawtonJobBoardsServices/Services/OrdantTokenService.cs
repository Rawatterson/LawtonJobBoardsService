using LawtonJobBoardsServices.Configuration;
using Microsoft.Extensions.Options;

namespace LawtonJobBoardsServices.Services;

/// <summary>
/// Manages the short-lived Ordant session token.
/// Authenticates on first use, refreshes from response headers, and
/// re-authenticates automatically when the token expires (HTTP 419).
/// </summary>
public class OrdantTokenService(IOptions<OrdantSettings> settings, IHttpClientFactory httpClientFactory)
{
    private volatile string? _token;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private readonly OrdantSettings _settings = settings.Value;

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        if (_token is not null)
            return _token;

        await _authLock.WaitAsync(ct);
        try
        {
            _token ??= await AuthenticateAsync(ct);
            return _token;
        }
        finally
        {
            _authLock.Release();
        }
    }

    /// <summary>Called after every successful API response to keep the token fresh.</summary>
    public void RefreshToken(string token) => _token = token;

    /// <summary>Called on HTTP 419 so the next GetTokenAsync re-authenticates.</summary>
    public void InvalidateToken() => _token = null;

    private async Task<string> AuthenticateAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/authenticate" +
                  $"?api_key={Uri.EscapeDataString(_settings.ApiKey)}" +
                  $"&api_user={Uri.EscapeDataString(_settings.ApiUser)}";

        var response = await client.PostAsJsonAsync(
            url,
            new { username = _settings.Username, password = _settings.Password },
            ct);

        response.EnsureSuccessStatusCode();

        if (!response.Headers.TryGetValues("X-Authentication", out var values))
            throw new InvalidOperationException(
                "Ordant authenticate response did not include an X-Authentication header.");

        return values.First();
    }
}
