namespace EnmyAuto.Api.Services.Media;

public interface IVideoRenderService
{
    /// <summary>
    /// Merges a still image and an audio file into an MP4 video.
    /// Both <paramref name="imageSource"/> and <paramref name="audioSource"/> may be
    /// local file paths or remote HTTP(S) URLs — the service handles downloading.
    /// </summary>
    /// <returns>Absolute path to the rendered MP4 file.</returns>
    Task<string> MergeMediaAsync(
        string imageSource,
        string audioSource,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
