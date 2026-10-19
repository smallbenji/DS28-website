using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Domain.Data.Configurations;
public class ActivityDriveFolderConfiguration : IEntityTypeConfiguration<ActivityDriveFolder>
{
    public void Configure(EntityTypeBuilder<ActivityDriveFolder> builder)
    {
        builder.ToTable("activity_drive_folder");

        builder.HasKey(x => x.ActivityId);

        builder.Property(x => x.DriveFolderId)
            .IsRequired();

        builder.HasOne(x => x.Activity)
            .WithOne()
            .HasForeignKey<ActivityDriveFolder>(x => x.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
