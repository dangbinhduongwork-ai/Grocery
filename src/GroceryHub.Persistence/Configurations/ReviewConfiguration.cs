using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", DatabaseSchemas.Reviews, t =>
        {
            t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_Reviews_Status", "[Status] IN ('Pending', 'Approved', 'Rejected')");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ProductId)
            .IsRequired();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.OrderItemId)
            .IsRequired();

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Title)
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(r => r.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(r => r.RejectionReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(r => r.ReplyBody)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired(false);

        builder.Property(r => r.EditedAt)
            .IsRequired(false);

        builder.Property(r => r.ModeratedAt)
            .IsRequired(false);

        builder.Property(r => r.ReplyAt)
            .IsRequired(false);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.OrderItem)
            .WithMany()
            .HasForeignKey(r => r.OrderItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Moderator)
            .WithMany()
            .HasForeignKey(r => r.ModeratedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Replier)
            .WithMany()
            .HasForeignKey(r => r.ReplyBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(r => r.OrderItemId)
            .IsUnique();

        builder.HasIndex(r => new { r.ProductId, r.Status, r.CreatedAt });
    }
}
