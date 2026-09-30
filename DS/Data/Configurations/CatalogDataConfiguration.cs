using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class CatalogDataConfiguration : IEntityTypeConfiguration<CatalogData>
{
    public void Configure(EntityTypeBuilder<CatalogData> builder)
    {
        builder.ToTable("catalog_data");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ActivityId)
            .IsUnique();

        builder.HasMany(x => x.Categories)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "catalog_data_category",
                right => right
                    .HasOne<ActivityCategory>()
                    .WithMany()
                    .HasForeignKey("activity_category_id")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<CatalogData>()
                    .WithMany()
                    .HasForeignKey("catalog_data_id")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("catalog_data_category");

                    join.HasKey("catalog_data_id", "activity_category_id");
                });

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ActivityId)
            .IsRequired();
    }
}
