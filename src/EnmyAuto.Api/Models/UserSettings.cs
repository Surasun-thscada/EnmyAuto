using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnmyAuto.Api.Models;

[Table("user_settings")]
public class UserSettings
{
    [Key, Column("user_id")]
    public Guid UserId { get; set; }

    // ── AI / Gemini ──────────────────────────────────────────────────────────
    [MaxLength(200), Column("gemini_api_key")]
    public string? GeminiApiKey { get; set; }

    [MaxLength(100), Column("gemini_model")]
    public string GeminiModel { get; set; } = "gemini-2.0-flash";

    [Column("gemini_temperature")]
    public float Temperature { get; set; } = 0.7f;

    [Column("gemini_max_tokens")]
    public int MaxOutputTokens { get; set; } = 1500;

    // ── Content Preferences ─────────────────────────────────────────────────
    [MaxLength(10), Column("content_language")]
    public string ContentLanguage { get; set; } = "th";

    [MaxLength(50), Column("content_tone")]
    public string ContentTone { get; set; } = "funny";

    [MaxLength(100), Column("default_category")]
    public string DefaultCategory { get; set; } = "";

    [Column("default_scene_count")]
    public int DefaultSceneCount { get; set; } = 5;

    // ── Navigation ──────────────────────────────────────────────────────────
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
