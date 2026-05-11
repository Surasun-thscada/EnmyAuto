using EnmyAuto.Api.Models.TikTok;

namespace EnmyAuto.Api.Services.TikTok;

public interface ITikTokPostService
{
    /// <summary>
    /// Uploads the MP4 at <paramref name="videoFilePath"/> to TikTok and publishes it.
    /// Returns the TikTok publish_id which can be polled for the final video_id.
    /// </summary>
    Task<string> PublishVideoAsync(
        string accessToken,
        string videoFilePath,
        string caption,
        string privacyLevel = "PUBLIC_TO_EVERYONE",
        CancellationToken ct = default);

    /// <summary>Polls TikTok until the video is processed or the call fails.</summary>
    Task<string> WaitForVideoIdAsync(
        string accessToken,
        string publishId,
        CancellationToken ct = default);
}
