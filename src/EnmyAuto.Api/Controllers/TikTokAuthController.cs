using System.Security.Claims;
using EnmyAuto.Api.Data;
using EnmyAuto.Api.Models;
using EnmyAuto.Api.Services.TikTok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnmyAuto.Api.Controllers;

[ApiController]
[Route("api/tiktok/auth")]
public sealed class TikTokAuthController(
    ITikTokAuthService authService,
    ApplicationDbContext db) : ControllerBase
{
    /// <summary>Returns connected TikTok account for the current user, or null.</summary>
    [Authorize]
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var userId  = GetCurrentUserId();
        var account = await db.TikTokAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        if (account is null) return Ok(new { connected = false });

        return Ok(new
        {
            connected      = true,
            accountId      = account.Id,
            tokenExpiresAt = account.TokenExpiresAt,
            connectedAt    = account.CreatedAt,
        });
    }

    /// <summary>Step 1 — redirect user to TikTok consent page (PKCE).</summary>
    [HttpGet("connect")]
    public IActionResult Connect([FromQuery] Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest(new { error = "userId is required." });

        var (url, _) = authService.BuildAuthorizationUrl(userId);
        return Redirect(url);
    }

    /// <summary>Disconnect (delete) the TikTok account for the current user.</summary>
    [Authorize]
    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        var userId  = GetCurrentUserId();
        var account = await db.TikTokAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        if (account is null) return NotFound(new { error = "No TikTok account connected." });

        db.TikTokAccounts.Remove(account);
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "TikTok account disconnected." });
    }

    /// <summary>Step 2 — TikTok redirects here after user grants permission.</summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = "Authorization code missing." });

        var parts = state?.Split('|') ?? [];
        if (parts.Length < 3 || !Guid.TryParse(parts[0], out var userId) || userId == Guid.Empty)
            return BadRequest(new { error = "Invalid state parameter." });

        var codeVerifier = parts[2];
        var tokens = await authService.ExchangeCodeAsync(code, codeVerifier, ct);

        var account = new TikTokAccount
        {
            UserId         = userId,
            AccessToken    = tokens.AccessToken,
            RefreshToken   = tokens.RefreshToken,
            TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn),
        };

        db.TikTokAccounts.Add(account);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            accountId = account.Id,
            openId    = tokens.OpenId,
            message   = "TikTok account connected successfully."
        });
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

}
