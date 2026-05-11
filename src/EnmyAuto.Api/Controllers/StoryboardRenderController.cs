using EnmyAuto.Api.Data;
using EnmyAuto.Api.Enums;
using EnmyAuto.Api.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnmyAuto.Api.Controllers;

[ApiController]
[Route("api/storyboards/{storyboardId:guid}/render")]
public sealed class StoryboardRenderController(
    ApplicationDbContext db,
    IBackgroundJobClient jobClient) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> EnqueueRender(
        Guid storyboardId,
        CancellationToken cancellationToken)
    {
        var storyboard = await db.Storyboards
            .Include(s => s.MediaAssets)
            .FirstOrDefaultAsync(s => s.Id == storyboardId, cancellationToken);

        if (storyboard is null)
            return NotFound(new { error = "Storyboard not found." });

        if (storyboard.Status == StoryboardStatus.Processing)
            return Conflict(new { error = "Render already in progress." });

        var imageAsset = storyboard.MediaAssets
            .FirstOrDefault(a => a.Type == MediaAssetType.Image);
        var audioAsset = storyboard.MediaAssets
            .FirstOrDefault(a => a.Type == MediaAssetType.Audio);

        if (imageAsset is null || audioAsset is null)
            return UnprocessableEntity(new
            {
                error = "Storyboard must have both an Image and an Audio asset before rendering."
            });

        storyboard.Status = StoryboardStatus.Processing;
        await db.SaveChangesAsync(cancellationToken);

        // Fire-and-forget — returns immediately; Hangfire runs the job in the background.
        var jobId = jobClient.Enqueue<VideoRenderJob>(job =>
            job.ExecuteAsync(storyboardId, imageAsset.FileUrl, audioAsset.FileUrl,
                CancellationToken.None));

        return Accepted(new
        {
            jobId,
            storyboardId,
            message = "Render enqueued. Connect to SignalR /hubs/render and subscribe to receive progress."
        });
    }
}
