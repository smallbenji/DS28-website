using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations;
public class CampDayConfiguration : IEntityTypeConfiguration<CampDay>
{
    public void Configure(EntityTypeBuilder<CampDay> builder)
    {
        builder.ToTable("camp_day", table =>
        {
            table.HasCheckConstraint("ck_camp_day_half", "half IN ('First', 'Second', 'Both')");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Date)
            .IsRequired();

        builder.HasIndex(x => x.Date)
            .IsUnique();

        builder.Property(x => x.Half)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
    }
}
