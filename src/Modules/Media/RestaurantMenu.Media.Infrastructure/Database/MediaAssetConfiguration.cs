using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Media.Domain.Assets;

namespace RestaurantMenu.Media.Infrastructure.Database;

internal sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets", table =>
        {
            table.HasCheckConstraint("ck_media_assets_size", $"size_bytes > 0 AND size_bytes <= {MediaAsset.MaxSizeBytes}");
            table.HasCheckConstraint("ck_media_assets_dimensions", "(width IS NULL AND height IS NULL) OR (width > 0 AND height > 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new MediaAssetId(x)).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.RestaurantId).HasColumnName("restaurant_id").IsRequired();
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(MediaAsset.MaxStorageKeyLength).IsRequired();
        builder.HasIndex(x => x.StorageKey).IsUnique().HasDatabaseName("ux_media_assets_storage_key");
        builder.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(MediaAsset.MaxFileNameLength).IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(MediaAsset.MaxContentTypeLength).IsRequired();
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(x => x.Width).HasColumnName("width");
        builder.Property(x => x.Height).HasColumnName("height");
        builder.Property(x => x.Sha256).HasColumnName("sha256").HasColumnType("character(64)").IsRequired();
        builder.HasIndex(x => new { x.RestaurantId, x.Sha256 }).IsUnique()
            .HasFilter("status IN ('Pending', 'Ready')").HasDatabaseName("ux_media_assets_restaurant_sha256_active");
        builder.Property(x => x.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(24).IsRequired();
        builder.Property(x => x.CreatedBySubject).HasColumnName("created_by_subject").HasMaxLength(255).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.Ignore(x => x.DomainEvents);
    }
}
