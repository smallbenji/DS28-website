using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class ActivityCategoryConfiguration : IEntityTypeConfiguration<ActivityCategory>
{
    public void Configure(EntityTypeBuilder<ActivityCategory> builder)
    {
        builder.ToTable("activity_category");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();
    }
}
