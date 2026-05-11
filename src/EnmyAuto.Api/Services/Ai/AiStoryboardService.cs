using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EnmyAuto.Api.Configuration;
using EnmyAuto.Api.Exceptions;
using EnmyAuto.Api.Models.Ai;
using Microsoft.Extensions.Options;

namespace EnmyAuto.Api.Services.Ai;

public sealed partial class AiStoryboardService(
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiOptions> options,
    ILogger<AiStoryboardService> logger) : IAiStoryboardService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    private const string SystemPrompt =
        """
        You are an expert e-commerce video scriptwriter specialising in short-form TikTok content.
        When given a product name and a content category, you respond ONLY with a single valid JSON
        object — no markdown fences, no commentary — that matches this exact schema:

        {
          "title": "<catchy video title>",
          "scenes": [
            {
              "scene_number": 1,
              "image_prompt": "<detailed visual prompt for AI image/video generation>",
              "voiceover_script": "<spoken text for this scene>",
              "duration_seconds": <integer 3-10>
            }
          ],
          "captions": "<full TikTok post caption>",
          "hashtags": ["#tag1", "#tag2"]
        }

        Rules:
        - Produce between 4 and 7 scenes.
        - Each image_prompt must be rich enough to feed directly to a diffusion model.
        - captions must be engaging, under 150 characters.
        - hashtags must be relevant and between 5 and 10 items.
        - Return ONLY the JSON — nothing else.
        """;

    public async Task<StoryboardScript> GenerateStoryboardAsync(
        string productName,
        string category,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        var opt = options.Value;
        var client = httpClientFactory.CreateClient(nameof(AiStoryboardService));

        var url = $"{opt.BaseUrl}/models/{opt.Model}:generateContent?key={opt.ApiKey}";
        var requestBody = BuildRequestPayload(productName, category, opt);

        logger.LogInformation(
            "Requesting storyboard from Gemini. Product={Product}, Category={Category}, Model={Model}",
            productName, category, opt.Model);

        using var response = await SendRequestAsync(client, url, requestBody, cancellationToken);
        var rawJson = await ExtractContentFromResponseAsync(response, cancellationToken);

        return ParseStoryboard(rawJson, productName, category);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static StringContent BuildRequestPayload(
        string productName, string category, GeminiOptions opt)
    {
        var userMessage =
            $"Product: {productName}\nCategory: {category}\n\nGenerate the TikTok storyboard JSON.";

        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = SystemPrompt } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                }
            },
            generationConfig = new
            {
                temperature       = opt.Temperature,
                maxOutputTokens   = opt.MaxOutputTokens,
                responseMimeType  = "application/json"  // forces Gemini to return pure JSON
            }
        };

        var json = JsonSerializer.Serialize(payload);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<HttpResponseMessage> SendRequestAsync(
        HttpClient client,
        string url,
        StringContent body,
        CancellationToken ct)
    {
        var response = await client.PostAsync(url, body, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new AiGenerationException(
                $"Gemini returned {(int)response.StatusCode}: {error}",
                (int)response.StatusCode);
        }

        return response;
    }

    private async Task<string> ExtractContentFromResponseAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        var responseText = await response.Content.ReadAsStringAsync(ct);

        JsonNode root;
        try
        {
            root = JsonNode.Parse(responseText)
                ?? throw new AiGenerationException("Gemini returned an empty JSON body.");
        }
        catch (JsonException ex)
        {
            throw new AiGenerationException("Failed to parse Gemini envelope JSON.", ex);
        }

        // Gemini response: candidates[0].content.parts[0].text
        var content = root["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            var finishReason = root["candidates"]?[0]?["finishReason"]?.GetValue<string>();
            logger.LogWarning("Gemini returned empty content. finishReason={Reason}", finishReason);
            throw new AiGenerationException(
                $"Gemini returned no content. finishReason={finishReason}");
        }

        return content;
    }

    private StoryboardScript ParseStoryboard(
        string rawJson, string productName, string category)
    {
        var cleaned = MarkdownFencePattern().Replace(rawJson, string.Empty).Trim();

        StoryboardScript? script;
        try
        {
            script = JsonSerializer.Deserialize<StoryboardScript>(cleaned, JsonOpts);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize storyboard JSON. Raw={Raw}", cleaned);
            throw new AiGenerationException(
                "Gemini returned JSON that does not match the expected storyboard schema.", ex);
        }

        if (script is null || script.Scenes.Count == 0)
            throw new AiGenerationException("Gemini returned a storyboard with no scenes.");

        logger.LogInformation(
            "Storyboard generated. Title={Title}, Scenes={Count}, Product={Product}, Category={Category}",
            script.Title, script.Scenes.Count, productName, category);

        return script;
    }

    [GeneratedRegex(@"```(?:json)?(.*?)```", RegexOptions.Singleline)]
    private static partial Regex MarkdownFencePattern();
}
