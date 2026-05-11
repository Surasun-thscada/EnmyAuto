using System.Security.Claims;
using EnmyAuto.Api.Data;
using EnmyAuto.Api.Models;
using EnmyAuto.Api.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnmyAuto.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public sealed class SettingsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId   = GetCurrentUserId();
        var settings = await db.UserSettings.FindAsync([userId], ct)
                    ?? new UserSettings { UserId = userId };

        return Ok(ToDto(settings));
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateSettingsRequest request, CancellationToken ct)
    {
        var userId   = GetCurrentUserId();
        var settings = await db.UserSettings.FindAsync([userId], ct);

        if (settings is null)
        {
            settings = new UserSettings { UserId = userId };
            db.UserSettings.Add(settings);
        }

        settings.GeminiApiKey     = string.IsNullOrWhiteSpace(request.GeminiApiKey)
                                        ? null
                                        : request.GeminiApiKey.Trim();
        settings.GeminiModel      = request.GeminiModel;
        settings.Temperature      = request.Temperature;
        settings.MaxOutputTokens  = request.MaxOutputTokens;
        settings.ContentLanguage  = request.ContentLanguage;
        settings.ContentTone      = request.ContentTone;
        settings.DefaultCategory  = request.DefaultCategory;
        settings.DefaultSceneCount = request.DefaultSceneCount;

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(settings));
    }

    private static UserSettingsDto ToDto(UserSettings s) => new(
        GeminiApiKey:      s.GeminiApiKey is null ? null : MaskApiKey(s.GeminiApiKey),
        GeminiModel:       s.GeminiModel,
        Temperature:       s.Temperature,
        MaxOutputTokens:   s.MaxOutputTokens,
        ContentLanguage:   s.ContentLanguage,
        ContentTone:       s.ContentTone,
        DefaultCategory:   s.DefaultCategory,
        DefaultSceneCount: s.DefaultSceneCount
    );

    // Show only last 4 chars of the API key for security
    private static string MaskApiKey(string key) =>
        key.Length > 4 ? $"{'•' + new string('•', Math.Min(key.Length - 4, 20))}{key[^4..]}" : "••••";

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
