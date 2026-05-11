using EnmyAuto.Api.Configuration;
using Microsoft.Extensions.Options;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

namespace EnmyAuto.Api.Services.Media;

public sealed class VideoRenderService(
    IHttpClientFactory httpClientFactory,
    IOptions<FfmpegOptions> options,
    ILogger<VideoRenderService> logger) : IVideoRenderService
{
    private readonly FfmpegOptions _opt = options.Value;

    public async Task<string> MergeMediaAsync(
        string imageSource,
        string audioSource,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(audioSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        FFmpeg.SetExecutablesPath(_opt.BinaryFolder);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        Directory.CreateDirectory(_opt.TempDirectory);

        // Resolve both sources — download if remote URL, use as-is if local path.
        var (imagePath, imageTempCreated) = await ResolveSourceAsync(
            imageSource, ".jpg", cancellationToken);
        var (audioPath, audioTempCreated) = await ResolveSourceAsync(
            audioSource, ".mp3", cancellationToken);

        try
        {
            progress?.Report(10);
            logger.LogInformation(
                "Starting FFmpeg render. Image={Image}, Audio={Audio}, Output={Output}",
                imagePath, audioPath, outputPath);

            await RunConversionAsync(imagePath, audioPath, outputPath, progress, cancellationToken);

            logger.LogInformation("Render complete. Output={Output}", outputPath);
            progress?.Report(100);

            return outputPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FFmpeg render failed. Output={Output}", outputPath);

            // Remove a partial/corrupt output file so callers get a clean failure.
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            throw;
        }
        finally
        {
            TryDelete(imagePath, imageTempCreated);
            TryDelete(audioPath, audioTempCreated);
        }
    }

    // ── FFmpeg conversion ─────────────────────────────────────────────────────

    private async Task RunConversionAsync(
        string imagePath,
        string audioPath,
        string outputPath,
        IProgress<int>? progress,
        CancellationToken ct)
    {
        // -loop 1          : treat the still image as an infinite input stream
        // -shortest        : stop encoding when the shorter stream (audio) ends
        // -tune stillimage : libx264 optimisation for single-frame sources
        // -pix_fmt yuv420p : broadest player compatibility (required for TikTok)
        var conversion = FFmpeg.Conversions.New()
            .AddParameter($"-loop 1", ParameterPosition.PreInput)
            .AddParameter($"-i \"{EscapePath(imagePath)}\"", ParameterPosition.PreInput)
            .AddParameter($"-i \"{EscapePath(audioPath)}\"", ParameterPosition.PreInput)
            .AddParameter($"-vf scale={_opt.VideoWidth}:{_opt.VideoHeight}:force_original_aspect_ratio=decrease,pad={_opt.VideoWidth}:{_opt.VideoHeight}:(ow-iw)/2:(oh-ih)/2")
            .AddParameter("-c:v libx264")
            .AddParameter("-tune stillimage")
            .AddParameter("-c:a aac")
            .AddParameter("-b:a 192k")
            .AddParameter("-pix_fmt yuv420p")
            .AddParameter("-movflags +faststart") // enables streaming before full download
            .AddParameter("-shortest")
            .SetOutput(outputPath);

        conversion.OnProgress += (_, args) =>
            ReportConversionProgress(args, progress);

        await conversion.Start(ct);
    }

    private static void ReportConversionProgress(
        ConversionProgressEventArgs args,
        IProgress<int>? progress)
    {
        // Map FFmpeg progress (0-100) to our 10-95 band, leaving 0-10 for download
        // and 95-100 for finalisation so the UI never appears to stall.
        var mapped = 10 + (int)(args.Percent * 0.85);
        progress?.Report(Math.Clamp(mapped, 10, 95));
    }

    // ── Source resolution ─────────────────────────────────────────────────────

    private async Task<(string Path, bool IsTempFile)> ResolveSourceAsync(
        string source, string extension, CancellationToken ct)
    {
        if (!IsRemoteUrl(source))
            return (source, false);

        var tempPath = Path.Combine(
            _opt.TempDirectory,
            $"{Guid.NewGuid()}{extension}");

        await DownloadFileAsync(source, tempPath, ct);

        logger.LogDebug("Downloaded {Url} → {TempPath}", source, tempPath);
        return (tempPath, true);
    }

    private async Task DownloadFileAsync(string url, string destinationPath, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(nameof(VideoRenderService));
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var file   = File.Create(destinationPath);
        await stream.CopyToAsync(file, ct);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static bool IsRemoteUrl(string source) =>
        source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string EscapePath(string path) =>
        path.Replace("\\", "/").Replace("'", "\\'");

    private static void TryDelete(string path, bool isTempFile)
    {
        if (!isTempFile) return;
        try   { File.Delete(path); }
        catch { /* best-effort cleanup */ }
    }
}
