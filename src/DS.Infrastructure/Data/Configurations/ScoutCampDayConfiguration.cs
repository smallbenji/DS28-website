using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations;
public class ScoutCampDayConfiguration : IEntityTypeConfiguration<ScoutCampDay>
{
    public void Configure(EntityTypeBuilder<ScoutCampDay> builder)
    {
        builder.ToTable("scout_camp_day");

        builder.HasKey(x => new { x.ScoutId, x.CampDayId });

        builder.HasIndex(x => x.CampDayId)
            .HasDatabaseName("ix_scout_camp_day_camp_day_id");

        builder.HasOne(x => x.Scout)
            .WithMany(x => x.CampDays)
            .HasForeignKey(x => x.ScoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CampDay)
            .WithMany()
            .HasForeignKey(x => x.CampDayId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
