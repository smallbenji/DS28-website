using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class OrgPositionHolderConfiguration : IEntityTypeConfiguration<OrgPositionHolder>
{
    public void Configure(EntityTypeBuilder<OrgPositionHolder> builder)
    {
        builder.ToTable("org_position_holder");

        builder.HasKey(x => new { x.PositionId, x.UserId });

        builder.HasOne(x => x.Position)
            .WithMany(x => x.Holders)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
    }
}
