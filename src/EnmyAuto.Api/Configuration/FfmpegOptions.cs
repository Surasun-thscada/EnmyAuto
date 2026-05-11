using System.ComponentModel.DataAnnotations;

namespace EnmyAuto.Api.Configuration;

public sealed class FfmpegOptions
{
    public const string SectionName = "FFmpeg";

    /// <summary>Folder containing the ffmpeg and ffprobe binaries.</summary>
    [Required]
    public string BinaryFolder { get; init; } = string.Empty;

    /// <summary>Directory used for downloading remote assets before processing.</summary>
    public string TempDirectory { get; init; } = Path.Combine(Path.GetTempPath(), "enmy_auto");

    /// <summary>Directory where finished MP4 files are written.</summary>
    [Required]
    public string OutputDirectory { get; init; } = string.Empty;

    /// <summary>Video width in pixels — must be even for libx264.</summary>
    public int VideoWidth { get; init; } = 1080;

    /// <summary>Video height in pixels — must be even for libx264.</summary>
    public int VideoHeight { get; init; } = 1920;
}
