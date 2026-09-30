using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class ActivityTimeslotConfiguration : IEntityTypeConfiguration<ActivityTimeslot>
{
    public void Configure(EntityTypeBuilder<ActivityTimeslot> builder)
    {
        builder.ToTable("activity_timeslots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.HasOne(x => x.Activity)
            .WithMany()
            .HasForeignKey(x => x.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}