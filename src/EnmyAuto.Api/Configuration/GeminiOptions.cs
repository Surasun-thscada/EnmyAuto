using System.ComponentModel.DataAnnotations;

namespace EnmyAuto.Api.Configuration;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "gemini-2.0-flash";

    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta";

    public int MaxOutputTokens { get; init; } = 1500;

    public double Temperature { get; init; } = 0.7;
}
