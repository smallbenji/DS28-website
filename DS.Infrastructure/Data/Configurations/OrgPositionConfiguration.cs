using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations;
public class OrgPositionConfiguration : IEntityTypeConfiguration<OrgPosition>
{
    public void Configure(EntityTypeBuilder<OrgPosition> builder)
    {
        builder.ToTable("org_position");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.IsSystem)
            .IsRequired()
            .HasDefaultValueSql("FALSE")
            .HasSentinel(false);

        builder.Property(x => x.Color)
            .HasMaxLength(16);

        builder.HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentPositionId);

        builder.HasOne(x => x.PaForPosition)
            .WithMany()
            .HasForeignKey(x => x.PaForPositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PaForPositionId)
            .IsUnique();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
