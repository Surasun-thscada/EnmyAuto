using System.Text.Json.Serialization;

namespace EnmyAuto.Api.Models.TikTok;

// ── OAuth ─────────────────────────────────────────────────────────────────────

public sealed record TikTokTokenResponse(
    [property: JsonPropertyName("access_token")]  string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")]    int    ExpiresIn,
    [property: JsonPropertyName("open_id")]       string OpenId,
    [property: JsonPropertyName("scope")]         string Scope);

public sealed record TikTokRefreshTokenResponse(
    [property: JsonPropertyName("access_token")]  string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")]    int    ExpiresIn);

// ── Video Publishing ──────────────────────────────────────────────────────────

public sealed class TikTokPublishRequest
{
    [JsonPropertyName("post_info")]
    public PostInfo PostInfo { get; init; } = new();

    [JsonPropertyName("source_info")]
    public SourceInfo SourceInfo { get; init; } = new();
}

public sealed class PostInfo
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("privacy_level")]
    public string PrivacyLevel { get; init; } = "PUBLIC_TO_EVERYONE";

    [JsonPropertyName("disable_duet")]
    public bool DisableDuet { get; init; } = false;

    [JsonPropertyName("disable_comment")]
    public bool DisableComment { get; init; } = false;

    [JsonPropertyName("disable_stitch")]
    public bool DisableStitch { get; init; } = false;
}

public sealed class SourceInfo
{
    [JsonPropertyName("source")]
    public string Source { get; init; } = "PULL_FROM_URL";

    [JsonPropertyName("video_url")]
    public string VideoUrl { get; init; } = string.Empty;

    [JsonPropertyName("video_size")]
    public long VideoSize { get; init; }

    [JsonPropertyName("chunk_size")]
    public long ChunkSize { get; init; }

    [JsonPropertyName("total_chunk_count")]
    public int TotalChunkCount { get; init; } = 1;
}

public sealed record TikTokPublishResponse(
    [property: JsonPropertyName("data")]  PublishData? Data,
    [property: JsonPropertyName("error")] TikTokError? Error);

public sealed record PublishData(
    [property: JsonPropertyName("publish_id")] string PublishId);

public sealed record TikTokError(
    [property: JsonPropertyName("code")]    string Code,
    [property: JsonPropertyName("message")] string Message);

// ── Publish Status ────────────────────────────────────────────────────────────

public sealed record TikTokPublishStatusRequest(
    [property: JsonPropertyName("publish_id")] string PublishId);

public sealed record TikTokPublishStatusResponse(
    [property: JsonPropertyName("data")]  PublishStatusData? Data,
    [property: JsonPropertyName("error")] TikTokError?       Error);

public sealed record PublishStatusData(
    [property: JsonPropertyName("status")]   string Status,
    [property: JsonPropertyName("video_id")] string? VideoId);

// ── Internal command passed to the job ───────────────────────────────────────

public sealed record PostVideoCommand(
    Guid   CampaignId,
    Guid   StoryboardId,
    Guid   TikTokAccountId,
    string VideoFilePath,
    string Caption,
    string PrivacyLevel);
