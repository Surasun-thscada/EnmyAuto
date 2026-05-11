using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnmyAuto.Api.Models;

[Table("tiktok_accounts")]
public class TikTokAccount
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    [Column("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// TikTok showcase product list ID linked to this account.
    /// </summary>
    [MaxLength(100)]
    [Column("showcase_id")]
    public string? ShowcaseId { get; set; }

    [Column("token_expires_at")]
    public DateTime? TokenExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<AutoPostCampaign> AutoPostCampaigns { get; set; } = [];
}
