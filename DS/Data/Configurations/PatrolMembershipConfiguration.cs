using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class PatrolMembershipConfiguration : IEntityTypeConfiguration<PatrolMembership>
{
    public void Configure(EntityTypeBuilder<PatrolMembership> builder)
    {
        builder.ToTable("patrol_membership");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Patrol)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.PatrolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Scout)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.ScoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ScoutId)
            .IsRequired();

        builder.Property(x => x.PatrolId)
            .IsRequired();

        builder.Property(x => x.JoinedDate)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.IsPatrolLeader)
            .IsRequired()
            .HasDefaultValueSql("FALSE")
            .HasSentinel(false);

        builder.HasIndex(x => new { x.ScoutId, x.PatrolId })
            .IsUnique();
    }
}
