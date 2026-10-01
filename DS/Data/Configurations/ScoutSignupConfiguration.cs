using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class ScoutSignupConfiguration : IEntityTypeConfiguration<ScoutSignup>
{
    public void Configure(EntityTypeBuilder<ScoutSignup> builder)
    {
        builder.ToTable("scout_signup");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Day)
            .IsRequired();

        builder.HasOne(x => x.Scout)
            .WithMany()
            .HasForeignKey(x => x.ScoutId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}