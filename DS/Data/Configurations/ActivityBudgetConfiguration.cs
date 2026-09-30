using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class ActivityBudgetConfiguration : IEntityTypeConfiguration<ActivityBudget>
{
    public void Configure(EntityTypeBuilder<ActivityBudget> builder)
    {
        builder.ToTable("activity_budget", table =>
        {
            table.HasCheckConstraint("ck_activity_budget_budget", "budget >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ActivityId)
            .IsUnique();

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Budget)
            .IsRequired()
            .HasDefaultValueSql("0");

        builder.Property(x => x.ActivityId)
            .IsRequired();
    }
}
