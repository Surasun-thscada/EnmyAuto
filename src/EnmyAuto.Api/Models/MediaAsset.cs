using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EnmyAuto.Api.Enums;

namespace EnmyAuto.Api.Models;

[Table("media_assets")]
public class MediaAsset
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("storyboard_id")]
    public Guid StoryboardId { get; set; }

    [Required]
    [Column("type")]
    public MediaAssetType Type { get; set; }

    [Required]
    [MaxLength(2048)]
    [Column("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes; populated after upload completes.
    /// </summary>
    [Column("file_size_bytes")]
    public long? FileSizeBytes { get; set; }

    [Column("duration_seconds")]
    public double? DurationSeconds { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(StoryboardId))]
    public Storyboard Storyboard { get; set; } = null!;
}
