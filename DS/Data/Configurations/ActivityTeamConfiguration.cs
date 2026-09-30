using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class ActivityTeamConfiguration : IEntityTypeConfiguration<ActivityTeam>
{
    public void Configure(EntityTypeBuilder<ActivityTeam> builder)
    {
        builder.ToTable("activity_team");

        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.Memberships)
            .WithOne(x => x.ActivityTeam)
            .HasForeignKey(x => x.ActivityTeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Name)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
