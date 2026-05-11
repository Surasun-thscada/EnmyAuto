using System.Text.Json.Serialization;

namespace EnmyAuto.Api.Models.Ai;

public sealed class SceneBreakdown
{
    [JsonPropertyName("scene_number")]
    public int SceneNumber { get; init; }

    /// <summary>Prompt sent to the image/video generation pipeline.</summary>
    [JsonPropertyName("image_prompt")]
    public string ImagePrompt { get; init; } = string.Empty;

    /// <summary>Text-to-speech script for this scene.</summary>
    [JsonPropertyName("voiceover_script")]
    public string VoiceoverScript { get; init; } = string.Empty;

    /// <summary>Approximate duration in seconds for pacing the scene.</summary>
    [JsonPropertyName("duration_seconds")]
    public int DurationSeconds { get; init; } = 5;
}
