using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnmyAuto.Api.Configuration;
using EnmyAuto.Api.Exceptions;
using EnmyAuto.Api.Models.TikTok;
using Microsoft.Extensions.Options;

namespace EnmyAuto.Api.Services.TikTok;

public sealed class TikTokPostService(
    IHttpClientFactory httpClientFactory,
    IOptions<TikTokOptions> options,
    ILogger<TikTokPostService> logger) : ITikTokPostService
{
    private readonly TikTokOptions _opt = options.Value;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string PublishInitUrl  = "https://open.tiktokapis.com/v2/post/publish/video/init/";
    private const string PublishStatusUrl = "https://open.tiktokapis.com/v2/post/publish/status/fetch/";

    // ── Publish ───────────────────────────────────────────────────────────────

    public async Task<string> PublishVideoAsync(
        string accessToken,
        string videoFilePath,
        string caption,
        string privacyLevel = "PUBLIC_TO_EVERYONE",
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoFilePath);

        if (!File.Exists(videoFilePath))
            throw new FileNotFoundException("Video file not found.", videoFilePath);

        var fileInfo  = new FileInfo(videoFilePath);
        var fileSize  = fileInfo.Length;

        logger.LogInformation(
            "Initiating TikTok publish. File={File}, Size={Size}B", videoFilePath, fileSize);

        // Step 1: Init upload — TikTok returns an upload URL
        var (publishId, uploadUrl) = await InitUploadAsync(
            accessToken, caption, privacyLevel, fileSize, ct);

        // Step 2: Upload the binary to TikTok's storage
        await UploadVideoChunkAsync(uploadUrl, videoFilePath, fileSize, ct);

        logger.LogInformation(
            "TikTok video upload complete. PublishId={PublishId}", publishId);

        return publishId;
    }

    // ── Poll for final video_id ───────────────────────────────────────────────

    public async Task<string> WaitForVideoIdAsync(
        string accessToken,
        string publishId,
        CancellationToken ct = default)
    {
        var client = BuildAuthorizedClient(accessToken);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(6), ct);

            var statusBody = JsonSerializer.Serialize(
                new TikTokPublishStatusRequest(publishId));

            using var response = await client.PostAsync(
                PublishStatusUrl,
                new StringContent(statusBody, Encoding.UTF8, "application/json"),
                ct);

            var json = await response.Content.ReadAsStringAsync(ct);
            var status = JsonSerializer.Deserialize<TikTokPublishStatusResponse>(json, JsonOpts);

            if (status?.Error is { Code: not "ok" } err)
                throw new AiGenerationException(
                    $"TikTok status check failed: [{err.Code}] {err.Message}");

            var result = status?.Data;

            logger.LogDebug(
                "TikTok publish status. PublishId={Id}, Status={Status}",
                publishId, result?.Status);

            switch (result?.Status)
            {
                case "PUBLISH_COMPLETE":
                    return result.VideoId
                        ?? throw new AiGenerationException(
                            "TikTok returned PUBLISH_COMPLETE but no video_id.");

                case "FAILED":
                    throw new AiGenerationException(
                        $"TikTok reported FAILED status for publish_id={publishId}.");
            }
            // PROCESSING_DOWNLOAD / PROCESSING_UPLOAD → keep polling
        }

        throw new AiGenerationException(
            $"TikTok publish timed out after 120 seconds. publish_id={publishId}");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<(string PublishId, string UploadUrl)> InitUploadAsync(
        string accessToken,
        string caption,
        string privacyLevel,
        long   fileSize,
        CancellationToken ct)
    {
        var request = new TikTokPublishRequest
        {
            PostInfo = new PostInfo
            {
                Title        = caption,
                PrivacyLevel = privacyLevel,
            },
            SourceInfo = new SourceInfo
            {
                Source           = "FILE_UPLOAD",
                VideoSize        = fileSize,
                ChunkSize        = fileSize,   // single chunk ≤ 64 MB
                TotalChunkCount  = 1,
            }
        };

        var body     = JsonSerializer.Serialize(request);
        var client   = BuildAuthorizedClient(accessToken);
        var response = await client.PostAsync(
            PublishInitUrl,
            new StringContent(body, Encoding.UTF8, "application/json"),
            ct);

        var json    = await response.Content.ReadAsStringAsync(ct);
        var result  = JsonSerializer.Deserialize<TikTokPublishResponse>(json, JsonOpts);

        if (!response.IsSuccessStatusCode || result?.Error is { Code: not "ok" } err)
        {
            var msg = result?.Error?.Message ?? json;
            throw new AiGenerationException($"TikTok init upload failed: {msg}");
        }

        var publishId = result?.Data?.PublishId
            ?? throw new AiGenerationException("TikTok did not return a publish_id.");

        // TikTok returns upload_url inside data for FILE_UPLOAD source
        var rawData  = JsonNode.Parse(json)?["data"];
        var uploadUrl = rawData?["upload_url"]?.GetValue<string>()
            ?? throw new AiGenerationException("TikTok did not return an upload_url.");

        return (publishId, uploadUrl);
    }

    private async Task UploadVideoChunkAsync(
        string uploadUrl,
        string videoFilePath,
        long   fileSize,
        CancellationToken ct)
    {
        var client  = httpClientFactory.CreateClient(nameof(TikTokPostService));
        client.Timeout = TimeSpan.FromMinutes(10); // large files need time

        await using var fileStream = File.OpenRead(videoFilePath);
        using var content = new StreamContent(fileStream);

        content.Headers.ContentType   = new MediaTypeHeaderValue("video/mp4");
        content.Headers.ContentLength = fileSize;
        content.Headers.ContentRange  =
            new ContentRangeHeaderValue(0, fileSize - 1, fileSize);

        var response = await client.PutAsync(uploadUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new AiGenerationException(
                $"TikTok chunk upload failed ({(int)response.StatusCode}): {error}");
        }
    }

    private HttpClient BuildAuthorizedClient(string accessToken)
    {
        var client = httpClientFactory.CreateClient(nameof(TikTokPostService));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
