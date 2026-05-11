using EnmyAuto.Api.Data;
using EnmyAuto.Api.Enums;
using EnmyAuto.Api.Jobs;
using EnmyAuto.Api.Models;
using EnmyAuto.Api.Models.TikTok;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnmyAuto.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
public sealed class AutoPostCampaignController(
    ApplicationDbContext db,
    IBackgroundJobClient jobClient) : ControllerBase
{
    // ── Create & schedule a campaign ─────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCampaignRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Validate storyboard has a rendered video asset
        var storyboard = await db.Storyboards
            .Include(s => s.MediaAssets)
            .FirstOrDefaultAsync(s => s.Id == request.StoryboardId, ct);

        if (storyboard is null)
            return NotFound(new { error = "Storyboard not found." });

        if (storyboard.Status != StoryboardStatus.Completed)
            return UnprocessableEntity(new
            {
                error = "Storyboard video must be fully rendered before scheduling a post."
            });

        var videoAsset = storyboard.MediaAssets
            .FirstOrDefault(a => a.Type == MediaAssetType.Video);

        if (videoAsset is null)
            return UnprocessableEntity(new
            {
                error = "No rendered video found on this storyboard."
            });

        var account = await db.TikTokAccounts.FindAsync([request.TikTokAccountId], ct);
        if (account is null)
            return NotFound(new { error = "TikTok account not found." });

        // Persist campaign record
        var campaign = new AutoPostCampaign
        {
            StoryboardId     = request.StoryboardId,
            TikTokAccountId  = request.TikTokAccountId,
            ScheduledTime    = request.ScheduledTime.ToUniversalTime(),
            Status           = CampaignStatus.Pending,
        };

        db.AutoPostCampaigns.Add(campaign);
        await db.SaveChangesAsync(ct);

        // Build caption from script captions + hashtags
        var caption = BuildCaption(request.CustomCaption, storyboard.ScriptJson);

        var command = new PostVideoCommand(
            CampaignId:     campaign.Id,
            StoryboardId:   storyboard.Id,
            TikTokAccountId: account.Id,
            VideoFilePath:  videoAsset.FileUrl,
            Caption:        caption,
            PrivacyLevel:   request.PrivacyLevel ?? "PUBLIC_TO_EVERYONE");

        // Schedule Hangfire job at the requested time
        var delay  = campaign.ScheduledTime - DateTime.UtcNow;
        var jobId  = delay > TimeSpan.Zero
            ? jobClient.Schedule<AutoPostJob>(
                job => job.ExecuteAsync(command, CancellationToken.None),
                delay)
            : jobClient.Enqueue<AutoPostJob>(
                job => job.ExecuteAsync(command, CancellationToken.None));

        return CreatedAtAction(nameof(GetById), new { id = campaign.Id }, new
        {
            campaignId    = campaign.Id,
            jobId,
            scheduledTime = campaign.ScheduledTime,
            status        = campaign.Status.ToString(),
        });
    }

    // ── Get single campaign ──────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var campaign = await db.AutoPostCampaigns
            .Include(c => c.Storyboard)
            .Include(c => c.TikTokAccount)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (campaign is null) return NotFound();

        return Ok(new
        {
            campaign.Id,
            campaign.Status,
            campaign.ScheduledTime,
            campaign.PublishedAt,
            campaign.TikTokVideoId,
            campaign.FailureReason,
            storyboardTitle  = campaign.Storyboard.Title,
        });
    }

    // ── List campaigns for a storyboard ─────────────────────────────────────

    [HttpGet("storyboard/{storyboardId:guid}")]
    public async Task<IActionResult> GetByStoryboard(Guid storyboardId, CancellationToken ct)
    {
        var campaigns = await db.AutoPostCampaigns
            .Where(c => c.StoryboardId == storyboardId)
            .OrderByDescending(c => c.ScheduledTime)
            .Select(c => new
            {
                c.Id,
                c.Status,
                c.ScheduledTime,
                c.PublishedAt,
                c.TikTokVideoId,
                c.FailureReason,
            })
            .ToListAsync(ct);

        return Ok(campaigns);
    }

    // ── Cancel a pending campaign ────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var campaign = await db.AutoPostCampaigns.FindAsync([id], ct);
        if (campaign is null) return NotFound();

        if (campaign.Status != CampaignStatus.Pending)
            return Conflict(new { error = "Only pending campaigns can be cancelled." });

        campaign.Status = CampaignStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return Ok(new { message = "Campaign cancelled." });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string BuildCaption(string? customCaption, string scriptJson)
    {
        if (!string.IsNullOrWhiteSpace(customCaption))
            return customCaption;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(scriptJson);
            var captions = doc.RootElement.GetProperty("captions").GetString() ?? string.Empty;
            var hashtags = doc.RootElement.GetProperty("hashtags")
                              .EnumerateArray()
                              .Select(h => h.GetString())
                              .Where(h => h != null);

            return $"{captions} {string.Join(" ", hashtags)}".Trim();
        }
        catch
        {
            return string.Empty;
        }
    }
}

public sealed record CreateCampaignRequest(
    [property: System.ComponentModel.DataAnnotations.Required]
    Guid StoryboardId,

    [property: System.ComponentModel.DataAnnotations.Required]
    Guid TikTokAccountId,

    [property: System.ComponentModel.DataAnnotations.Required]
    DateTime ScheduledTime,

    string? CustomCaption  = null,
    string? PrivacyLevel   = "PUBLIC_TO_EVERYONE");
