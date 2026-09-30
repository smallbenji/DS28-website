using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
{
    public void Configure(EntityTypeBuilder<UserInvitation> builder)
    {
        builder.ToTable("user_invitation");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.InvitationId)
            .IsUnique();

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.InvitationId)
            .IsRequired();

        builder.Property(x => x.Email)
            .IsRequired();

        builder.Property(x => x.Roles)
            .IsRequired()
            .HasDefaultValueSql("'{}'");

        builder.Property(x => x.Used)
            .IsRequired()
            .HasDefaultValueSql("FALSE");

        builder.Property(x => x.IsAdmin)
            .IsRequired()
            .HasDefaultValueSql("FALSE");

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasOne<ActivityTeam>()
            .WithMany()
            .HasForeignKey(x => x.ActivityTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
