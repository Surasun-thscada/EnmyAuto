using System.ComponentModel.DataAnnotations;

namespace EnmyAuto.Api.Configuration;

public sealed class TikTokOptions
{
    public const string SectionName = "TikTok";

    [Required] public string ClientKey    { get; init; } = string.Empty;
    [Required] public string ClientSecret { get; init; } = string.Empty;
    [Required] public string RedirectUri  { get; init; } = string.Empty;

    public string BaseUrl       { get; init; } = "https://open.tiktokapis.com";
    public string AuthUrl       { get; init; } = "https://www.tiktok.com/v2/auth/authorize";
    public string TokenUrl      { get; init; } = "https://open.tiktokapis.com/v2/oauth/token/";

    /// <summary>Default privacy for posted videos.</summary>
    public string DefaultPrivacyLevel { get; init; } = "PUBLIC_TO_EVERYONE";
}
