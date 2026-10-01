using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("activity");

        builder.HasKey(x => x.Id);

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
