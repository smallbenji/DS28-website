using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations;
public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("activity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReviewStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(ActivityReviewStatus.Draft);

        builder.Property(x => x.ReviewVersion)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.Property(x => x.ReviewFeedback)
            .HasMaxLength(4000);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.ReviewStatus, x.SubmittedAt });

        builder.HasOne(x => x.ActivityTeam)
            .WithMany(x => x.Activities)
            .HasForeignKey(x => x.ActivityTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Budget)
            .WithOne(x => x.Activity)
            .HasForeignKey<ActivityBudget>(x => x.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Catalog)
            .WithOne(x => x.Activity)
            .HasForeignKey<CatalogData>(x => x.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ActivityTeamId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
