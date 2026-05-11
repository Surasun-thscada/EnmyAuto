using System.Text.Json.Serialization;

namespace EnmyAuto.Api.Models.Ai;

public sealed class StoryboardScript
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("scenes")]
    public List<SceneBreakdown> Scenes { get; init; } = [];

    [JsonPropertyName("captions")]
    public string Captions { get; init; } = string.Empty;

    [JsonPropertyName("hashtags")]
    public List<string> Hashtags { get; init; } = [];
}
