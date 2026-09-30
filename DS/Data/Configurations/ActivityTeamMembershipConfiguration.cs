using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class ActivityTeamMembershipConfiguration : IEntityTypeConfiguration<ActivityTeamMembership>
{
    public void Configure(EntityTypeBuilder<ActivityTeamMembership> builder)
    {
        builder.ToTable("activity_team_membership");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.ActivityTeamId
        }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.ActivityTeamId)
            .IsRequired();

        builder.Property(x => x.IsAdmin)
            .IsRequired()
            .HasDefaultValueSql("FALSE");
    }
}
