using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnmyAuto.Api.Configuration;
using EnmyAuto.Api.Data;
using EnmyAuto.Api.Exceptions;
using EnmyAuto.Api.Models.TikTok;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnmyAuto.Api.Services.TikTok;

public sealed class TikTokAuthService(
    IHttpClientFactory httpClientFactory,
    IOptions<TikTokOptions> options,
    ApplicationDbContext db,
    ILogger<TikTokAuthService> logger) : ITikTokAuthService
{
    private readonly TikTokOptions _opt = options.Value;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Authorization URL (PKCE) ──────────────────────────────────────────────

    public (string Url, string State) BuildAuthorizationUrl(Guid userId)
    {
        var verifier  = GenerateCodeVerifier();
        var challenge = GenerateCodeChallenge(verifier);
        // Embed verifier in state so it survives the redirect round-trip.
        var state  = $"{userId}|{Guid.NewGuid()}|{verifier}";
        var scopes = "user.info.basic,video.publish,video.upload";

        var url = $"{_opt.AuthUrl}" +
                  $"?client_key={Uri.EscapeDataString(_opt.ClientKey)}" +
                  $"&scope={Uri.EscapeDataString(scopes)}" +
                  $"&response_type=code" +
                  $"&redirect_uri={Uri.EscapeDataString(_opt.RedirectUri)}" +
                  $"&state={Uri.EscapeDataString(state)}" +
                  $"&code_challenge={Uri.EscapeDataString(challenge)}" +
                  $"&code_challenge_method=S256";

        return (url, state);
    }

    // ── Code Exchange ─────────────────────────────────────────────────────────

    public async Task<TikTokTokenResponse> ExchangeCodeAsync(
        string code, string codeVerifier, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient(nameof(TikTokAuthService));

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_key"]     = _opt.ClientKey,
            ["client_secret"]  = _opt.ClientSecret,
            ["code"]           = code,
            ["grant_type"]     = "authorization_code",
            ["redirect_uri"]   = _opt.RedirectUri,
            ["code_verifier"]  = codeVerifier,
        });

        var response = await client.PostAsync(_opt.TokenUrl, body, ct);
        var json     = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new AiGenerationException($"TikTok token exchange failed: {json}");

        return JsonSerializer.Deserialize<TikTokTokenResponse>(json, JsonOpts)
            ?? throw new AiGenerationException("TikTok returned an empty token response.");
    }

    // ── Token Refresh ─────────────────────────────────────────────────────────

    public async Task<string> GetValidAccessTokenAsync(
        Guid accountId, CancellationToken ct = default)
    {
        var account = await db.TikTokAccounts.FindAsync([accountId], ct)
            ?? throw new InvalidOperationException($"TikTok account {accountId} not found.");

        // Return current token if still valid with a 5-minute buffer.
        if (account.TokenExpiresAt.HasValue &&
            account.TokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
        {
            return account.AccessToken;
        }

        logger.LogInformation("Refreshing TikTok token for account {Id}", accountId);

        var client = httpClientFactory.CreateClient(nameof(TikTokAuthService));

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_key"]     = _opt.ClientKey,
            ["client_secret"]  = _opt.ClientSecret,
            ["grant_type"]     = "refresh_token",
            ["refresh_token"]  = account.RefreshToken,
        });

        var response = await client.PostAsync(_opt.TokenUrl, body, ct);
        var json     = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new AiGenerationException($"TikTok token refresh failed: {json}");

        var refreshed = JsonSerializer.Deserialize<TikTokRefreshTokenResponse>(json, JsonOpts)
            ?? throw new AiGenerationException("TikTok returned empty refresh token response.");

        account.AccessToken     = refreshed.AccessToken;
        account.RefreshToken    = refreshed.RefreshToken;
        account.TokenExpiresAt  = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn);
        await db.SaveChangesAsync(ct);

        return refreshed.AccessToken;
    }

    // ── PKCE helpers ──────────────────────────────────────────────────────────

    private static string GenerateCodeVerifier()
        => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string GenerateCodeChallenge(string verifier)
        => Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
