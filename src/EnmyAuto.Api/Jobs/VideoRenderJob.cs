using EnmyAuto.Api.Configuration;
using EnmyAuto.Api.Data;
using EnmyAuto.Api.Enums;
using EnmyAuto.Api.Hubs;
using EnmyAuto.Api.Models.Hubs;
using EnmyAuto.Api.Services.Media;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnmyAuto.Api.Jobs;

public sealed class VideoRenderJob(
    IVideoRenderService videoRenderService,
    IHubContext<RenderHub> hubContext,
    ApplicationDbContext db,
    IOptions<FfmpegOptions> ffmpegOptions,
    ILogger<VideoRenderJob> logger)
{
    private readonly FfmpegOptions _ffmpegOpt = ffmpegOptions.Value;

    // ── Public entry point — called by Hangfire ───────────────────────────────

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task ExecuteAsync(
        Guid storyboardId,
        string imageSource,
        string audioSource,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("VideoRenderJob started. StoryboardId={Id}", storyboardId);

        await NotifyAsync(storyboardId, RenderEventType.Started, 0);

        var outputPath = BuildOutputPath(storyboardId);

        try
        {
            // Wire FFmpeg progress events → SignalR group push
            var signalRProgress = new Progress<int>(async pct =>
                await NotifyAsync(storyboardId, RenderEventType.Progress, pct));

            var renderedPath = await videoRenderService.MergeMediaAsync(
                imageSource,
                audioSource,
                outputPath,
                signalRProgress,
                cancellationToken);

            await PersistMediaAssetAsync(storyboardId, renderedPath, cancellationToken);

            await NotifyAsync(storyboardId, RenderEventType.Completed, 100, outputPath: renderedPath);

            logger.LogInformation(
                "VideoRenderJob completed. StoryboardId={Id}, Output={Path}",
                storyboardId, renderedPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VideoRenderJob failed. StoryboardId={Id}", storyboardId);

            await MarkStoryboardFailedAsync(storyboardId, cancellationToken);

            await NotifyAsync(
                storyboardId,
                RenderEventType.Failed,
                progress: 0,
                errorMessage: ex.Message);

            // Re-throw so Hangfire records the failure and honours AutomaticRetry.
            throw;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string BuildOutputPath(Guid storyboardId) =>
        Path.Combine(_ffmpegOpt.OutputDirectory, $"{storyboardId}.mp4");

    private async Task PersistMediaAssetAsync(
        Guid storyboardId,
        string outputPath,
        CancellationToken ct)
    {
        var storyboard = await db.Storyboards.FindAsync([storyboardId], ct)
            ?? throw new InvalidOperationException($"Storyboard {storyboardId} not found.");

        storyboard.Status = StoryboardStatus.Completed;

        db.MediaAssets.Add(new Models.MediaAsset
        {
            StoryboardId = storyboardId,
            Type         = Enums.MediaAssetType.Video,
            FileUrl      = outputPath,
            FileSizeBytes = new FileInfo(outputPath).Length
        });

        await db.SaveChangesAsync(ct);
    }

    private async Task MarkStoryboardFailedAsync(Guid storyboardId, CancellationToken ct)
    {
        var storyboard = await db.Storyboards
            .FirstOrDefaultAsync(s => s.Id == storyboardId, ct);

        if (storyboard is null) return;

        storyboard.Status = StoryboardStatus.Failed;
        await db.SaveChangesAsync(ct);
    }

    private Task NotifyAsync(
        Guid storyboardId,
        RenderEventType eventType,
        int progress,
        string? outputPath = null,
        string? errorMessage = null)
    {
        var payload = new RenderProgressEvent(
            StoryboardId:    storyboardId,
            EventType:       eventType,
            ProgressPercent: progress,
            OutputPath:      outputPath,
            ErrorMessage:    errorMessage,
            Timestamp:       DateTime.UtcNow);

        return hubContext.Clients
            .Group(RenderHub.GroupName(storyboardId))
            .SendAsync("onRenderProgress", payload);
    }
}
