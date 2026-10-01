using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class RegistrationSettingsConfiguration : IEntityTypeConfiguration<RegistrationSettings>
{
    public void Configure(EntityTypeBuilder<RegistrationSettings> builder)
    {
        builder.ToTable("registration_settings", table =>
        {
            table.HasCheckConstraint("ck_registration_settings_id", "id = 1");
        });

        builder.HasKey(x => x.Id);

        builder.HasData(new RegistrationSettings
        {
            Id = 1,
            IsPreSignupOpen = false
        });

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .HasDefaultValueSql("1");

        builder.Property(x => x.IsPreSignupOpen)
            .IsRequired()
            .HasDefaultValueSql("FALSE")
            .HasSentinel(false);

        builder.Property(x => x.IsSignupOpen)
            .IsRequired()
            .HasDefaultValueSql("FALSE")
            .HasSentinel(false);
    }
}
