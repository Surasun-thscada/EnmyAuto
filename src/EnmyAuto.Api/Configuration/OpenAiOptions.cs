using System.ComponentModel.DataAnnotations;

namespace EnmyAuto.Api.Configuration;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = "gpt-4o";

    public string BaseUrl { get; init; } = "https://api.openai.com/v1";

    /// <summary>Max tokens allocated for the storyboard JSON response.</summary>
    public int MaxTokens { get; init; } = 1500;

    /// <summary>Sampling temperature — lower = more deterministic output.</summary>
    public double Temperature { get; init; } = 0.7;
}
