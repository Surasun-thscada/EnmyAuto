using EnmyAuto.Api.Data;
using EnmyAuto.Api.Enums;
using EnmyAuto.Api.Models.TikTok;
using EnmyAuto.Api.Services.TikTok;
using Hangfire;

namespace EnmyAuto.Api.Jobs;

public sealed class AutoPostJob(
    ITikTokAuthService authService,
    ITikTokPostService postService,
    ApplicationDbContext db,
    ILogger<AutoPostJob> logger)
{
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task ExecuteAsync(
        PostVideoCommand command,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "AutoPostJob started. CampaignId={CampaignId}", command.CampaignId);

        var campaign = await db.AutoPostCampaigns.FindAsync([command.CampaignId], ct)
            ?? throw new InvalidOperationException(
                $"Campaign {command.CampaignId} not found.");

        try
        {
            campaign.Status = CampaignStatus.Scheduled;
            await db.SaveChangesAsync(ct);

            // 1. Get a valid (possibly refreshed) access token
            var accessToken = await authService.GetValidAccessTokenAsync(
                command.TikTokAccountId, ct);

            // 2. Upload video to TikTok and get publish_id
            var publishId = await postService.PublishVideoAsync(
                accessToken,
                command.VideoFilePath,
                command.Caption,
                command.PrivacyLevel,
                ct);

            // 3. Poll until TikTok finishes processing → get video_id
            var videoId = await postService.WaitForVideoIdAsync(
                accessToken, publishId, ct);

            // 4. Mark campaign as published
            campaign.Status        = CampaignStatus.Published;
            campaign.TikTokVideoId = videoId;
            campaign.PublishedAt   = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "AutoPostJob complete. CampaignId={CampaignId}, VideoId={VideoId}",
                command.CampaignId, videoId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "AutoPostJob failed. CampaignId={CampaignId}", command.CampaignId);

            campaign.Status        = CampaignStatus.Failed;
            campaign.FailureReason = ex.Message;
            await db.SaveChangesAsync(ct);

            throw; // allow Hangfire to retry
        }
    }
}
