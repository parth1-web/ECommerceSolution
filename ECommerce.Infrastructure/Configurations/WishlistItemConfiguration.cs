using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public class WishlistItemConfiguration
    : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(
        EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");

        builder.HasKey(wi => wi.Id);

        builder.Property(wi => wi.Id)
            .ValueGeneratedOnAdd();

        builder.Property(wi => wi.WishlistId)
            .IsRequired();

        builder.Property(wi => wi.ProductId)
            .IsRequired();

        builder.Property(wi => wi.CreatedAt)
            .IsRequired();

        builder.HasOne(wi => wi.Wishlist)
            .WithMany(w => w.WishlistItems)
            .HasForeignKey(wi => wi.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wi => wi.Product)
            .WithMany()
            .HasForeignKey(wi => wi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(wi => new
        {
            wi.WishlistId,
            wi.ProductId
        })
        .IsUnique();
    }
}