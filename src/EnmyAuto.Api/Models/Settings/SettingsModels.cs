using System.ComponentModel.DataAnnotations;

namespace EnmyAuto.Api.Models.Settings;

public sealed record UserSettingsDto(
    string?  GeminiApiKey,
    string   GeminiModel,
    float    Temperature,
    int      MaxOutputTokens,
    string   ContentLanguage,
    string   ContentTone,
    string   DefaultCategory,
    int      DefaultSceneCount
);

public sealed record UpdateSettingsRequest(
    [MaxLength(200)]  string?  GeminiApiKey,
    [MaxLength(100)]  string   GeminiModel,
    [Range(0f, 2f)]   float    Temperature,
    [Range(100, 8192)] int     MaxOutputTokens,
    [MaxLength(10)]   string   ContentLanguage,
    [MaxLength(50)]   string   ContentTone,
    [MaxLength(100)]  string   DefaultCategory,
    [Range(1, 10)]    int      DefaultSceneCount
);
