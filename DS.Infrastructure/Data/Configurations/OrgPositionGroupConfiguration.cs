using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations;
public class OrgPositionGroupConfiguration : IEntityTypeConfiguration<OrgPositionGroup>
{
    public void Configure(EntityTypeBuilder<OrgPositionGroup> builder)
    {
        builder.ToTable("org_position_group");

        builder.HasKey(x => new { x.PositionId, x.GroupName });

        builder.Property(x => x.GroupName)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne(x => x.Position)
            .WithMany(x => x.Groups)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
