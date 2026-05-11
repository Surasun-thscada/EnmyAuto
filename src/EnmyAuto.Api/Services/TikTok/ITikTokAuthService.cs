using EnmyAuto.Api.Models.TikTok;

namespace EnmyAuto.Api.Services.TikTok;

public interface ITikTokAuthService
{
    /// <summary>Builds the TikTok OAuth consent URL (PKCE). Returns the redirect URL and the state string that embeds the code verifier.</summary>
    (string Url, string State) BuildAuthorizationUrl(Guid userId);

    /// <summary>Exchanges the authorization code for access + refresh tokens.</summary>
    Task<TikTokTokenResponse> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken ct = default);

    /// <summary>
    /// Returns a valid access token for the account, refreshing it first if expired.
    /// </summary>
    Task<string> GetValidAccessTokenAsync(Guid accountId, CancellationToken ct = default);
}
