using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EnmyAuto.Api.Enums;

namespace EnmyAuto.Api.Models;

[Table("auto_post_campaigns")]
public class AutoPostCampaign
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("storyboard_id")]
    public Guid StoryboardId { get; set; }

    [Required]
    [Column("tiktok_account_id")]
    public Guid TikTokAccountId { get; set; }

    [Required]
    [Column("scheduled_time")]
    public DateTime ScheduledTime { get; set; }

    [Required]
    [Column("status")]
    public CampaignStatus Status { get; set; } = CampaignStatus.Pending;

    /// <summary>
    /// TikTok video ID returned after a successful publish.
    /// </summary>
    [MaxLength(100)]
    [Column("tiktok_video_id")]
    public string? TikTokVideoId { get; set; }

    [MaxLength(1000)]
    [Column("failure_reason")]
    public string? FailureReason { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(StoryboardId))]
    public Storyboard Storyboard { get; set; } = null!;

    [ForeignKey(nameof(TikTokAccountId))]
    public TikTokAccount TikTokAccount { get; set; } = null!;
}
