using Channels.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Channels.Infrastructure.Persistence.Configurations;

/// <summary>ProductListing aggregate kökünün EF Core eşleme yapılandırması.</summary>
internal sealed class ProductListingConfiguration : IEntityTypeConfiguration<ProductListing>
{
    public void Configure(EntityTypeBuilder<ProductListing> builder)
    {
        builder.ToTable("product_listings");

        builder.HasKey(listing => listing.Id);
        builder.Property(listing => listing.Id).HasColumnName("id");
        builder.Ignore(listing => listing.DomainEvents);
        builder.Ignore(listing => listing.IsDirty);

        builder.Property(listing => listing.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(listing => listing.Marketplace)
            .ConfigureMarketplaceColumn();

        builder.Property(listing => listing.ProductItemId)
            .HasColumnName("product_item_id")
            .IsRequired();

        builder.Property(listing => listing.Status)
            .HasColumnName("status")
            .HasConversion(
                status => ToPersistence(status),
                value => FromPersistence(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(listing => listing.ExternalListingId)
            .HasColumnName("external_listing_id")
            .HasMaxLength(ProductListing.ExternalListingIdMaxLength);

        builder.Property(listing => listing.SubmissionReference)
            .HasColumnName("submission_reference")
            .HasMaxLength(ProductListing.SubmissionReferenceMaxLength);

        builder.Property(listing => listing.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(ProductListing.HashMaxLength);

        builder.Property(listing => listing.OfferHash)
            .HasColumnName("offer_hash")
            .HasMaxLength(ProductListing.HashMaxLength);

        builder.Property(listing => listing.ContentDirtyAt).HasColumnName("content_dirty_at");
        builder.Property(listing => listing.OfferDirtyAt).HasColumnName("offer_dirty_at");
        builder.Property(listing => listing.LastSubmittedAt).HasColumnName("last_submitted_at");
        builder.Property(listing => listing.LastConfirmedAt).HasColumnName("last_confirmed_at");

        builder.Property(listing => listing.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(ProductListing.RejectionReasonMaxLength);

        builder.Property(listing => listing.SyncAttempts).HasColumnName("sync_attempts").IsRequired();
        builder.Property(listing => listing.NextAttemptAt).HasColumnName("next_attempt_at");

        // Aggregate'in doğal anahtarı: aynı kalem aynı pazaryerinde iki kez listelenemez.
        builder
            .HasIndex(listing => new { listing.TenantId, listing.Marketplace, listing.ProductItemId })
            .IsUnique();

        // Kirlilik işaretlemesi kalem kimliğinden girer (olay yalnız ID taşır).
        builder.HasIndex(listing => listing.ProductItemId);

        // Senkron pompasının erişim yolu: yalnız kirli satırlar üzerinde kısmi index.
        builder
            .HasIndex(listing => new { listing.TenantId, listing.Marketplace })
            .HasDatabaseName("ix_product_listings_dirty")
            .HasFilter("content_dirty_at IS NOT NULL OR offer_dirty_at IS NOT NULL");
    }

    private static string ToPersistence(ListingStatus status) =>
        status switch
        {
            ListingStatus.Pending => "pending",
            ListingStatus.Submitted => "submitted",
            ListingStatus.Live => "live",
            ListingStatus.Rejected => "rejected",
            ListingStatus.PendingDelist => "pending_delist",
            ListingStatus.Delisted => "delisted",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    private static ListingStatus FromPersistence(string value) =>
        value switch
        {
            "pending" => ListingStatus.Pending,
            "submitted" => ListingStatus.Submitted,
            "live" => ListingStatus.Live,
            "rejected" => ListingStatus.Rejected,
            "pending_delist" => ListingStatus.PendingDelist,
            "delisted" => ListingStatus.Delisted,
            _ => ListingStatus.Pending,
        };
}
