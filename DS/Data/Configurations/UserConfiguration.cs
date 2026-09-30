using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("asp_net_users");

        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey("GroupId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.UserName)
            .HasMaxLength(256);

        builder.Property(x => x.NormalizedUserName)
            .HasMaxLength(256);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.NormalizedEmail)
            .HasMaxLength(256);

        builder.Property(x => x.EmailConfirmed)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(256);

        builder.Property(x => x.PhoneNumberConfirmed)
            .IsRequired();

        builder.Property(x => x.TwoFactorEnabled)
            .IsRequired();

        builder.Property(x => x.LockoutEnabled)
            .IsRequired();

        builder.Property(x => x.AccessFailedCount)
            .IsRequired();

        builder.Property(x => x.HasEnabledAuthenticator)
            .IsRequired()
            .HasDefaultValueSql("FALSE");
    }
}
