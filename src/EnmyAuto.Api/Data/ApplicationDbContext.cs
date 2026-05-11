using EnmyAuto.Api.Enums;
using EnmyAuto.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EnmyAuto.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<TikTokAccount> TikTokAccounts => Set<TikTokAccount>();
    public DbSet<Storyboard> Storyboards => Set<Storyboard>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<AutoPostCampaign> AutoPostCampaigns => Set<AutoPostCampaign>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("ix_users_email");

            entity.Property(u => u.QuotaLimit)
                  .HasDefaultValue(10);
        });

        // ── UserSettings ──────────────────────────────────────────────────────
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasOne(s => s.User)
                  .WithOne()
                  .HasForeignKey<UserSettings>(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(s => s.Temperature).HasColumnType("real");
        });

        // ── TikTokAccount ─────────────────────────────────────────────────────
        modelBuilder.Entity<TikTokAccount>(entity =>
        {
            entity.HasOne(t => t.User)
                  .WithMany(u => u.TikTokAccounts)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Tokens are stored encrypted at the application layer; keep them as text.
            entity.Property(t => t.AccessToken).HasColumnType("text");
            entity.Property(t => t.RefreshToken).HasColumnType("text");

            entity.HasIndex(t => t.UserId)
                  .HasDatabaseName("ix_tiktok_accounts_user_id");
        });

        // ── Storyboard ────────────────────────────────────────────────────────
        modelBuilder.Entity<Storyboard>(entity =>
        {
            entity.HasOne(s => s.User)
                  .WithMany(u => u.Storyboards)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(s => s.Category)
                  .HasConversion(new EnumToStringConverter<StoryboardCategory>());

            entity.Property(s => s.Status)
                  .HasConversion(new EnumToStringConverter<StoryboardStatus>());

            entity.HasIndex(s => new { s.UserId, s.Status })
                  .HasDatabaseName("ix_storyboards_user_status");

            // ScriptJson is already mapped to jsonb via TypeName on the property.
        });

        // ── MediaAsset ────────────────────────────────────────────────────────
        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasOne(m => m.Storyboard)
                  .WithMany(s => s.MediaAssets)
                  .HasForeignKey(m => m.StoryboardId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(m => m.Type)
                  .HasConversion(new EnumToStringConverter<MediaAssetType>());

            entity.HasIndex(m => new { m.StoryboardId, m.Type })
                  .HasDatabaseName("ix_media_assets_storyboard_type");
        });

        // ── AutoPostCampaign ──────────────────────────────────────────────────
        modelBuilder.Entity<AutoPostCampaign>(entity =>
        {
            entity.HasOne(c => c.Storyboard)
                  .WithMany(s => s.AutoPostCampaigns)
                  .HasForeignKey(c => c.StoryboardId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.TikTokAccount)
                  .WithMany(t => t.AutoPostCampaigns)
                  .HasForeignKey(c => c.TikTokAccountId)
                  .OnDelete(DeleteBehavior.Restrict); // keep campaign record if account is removed

            entity.Property(c => c.Status)
                  .HasConversion(new EnumToStringConverter<CampaignStatus>());

            entity.HasIndex(c => new { c.TikTokAccountId, c.ScheduledTime })
                  .HasDatabaseName("ix_campaigns_account_scheduled");

            entity.HasIndex(c => c.Status)
                  .HasDatabaseName("ix_campaigns_status");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampUpdatedAt();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampUpdatedAt();
        return base.SaveChanges();
    }

    private void StampUpdatedAt()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Storyboard sb)
                sb.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is AutoPostCampaign campaign)
                campaign.UpdatedAt = DateTime.UtcNow;
        }
    }
}
