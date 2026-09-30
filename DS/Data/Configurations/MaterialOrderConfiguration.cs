using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class MaterialOrderConfiguration : IEntityTypeConfiguration<MaterialOrder>
{
    public void Configure(EntityTypeBuilder<MaterialOrder> builder)
    {
        builder.ToTable("material_order", table =>
        {
            table.HasCheckConstraint("ck_material_order_quantity", "quantity > 0");
        });

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Activity)
            .WithMany()
            .HasForeignKey("ActivityId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Material)
            .WithMany()
            .HasForeignKey("MaterialId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property<int>("ActivityId")
            .IsRequired();

        builder.Property<int>("MaterialId")
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasDefaultValueSql("1");

        builder.Property(x => x.OrderedToDate)
            .HasColumnName("use_date")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
