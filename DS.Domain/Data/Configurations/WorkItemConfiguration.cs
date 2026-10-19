using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Domain.Data.Configurations;
public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("work_item");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .HasColumnType("date");

        builder.Property(x => x.Color)
            .HasMaxLength(7);

        builder.Property(x => x.Priority)
            .IsRequired()
            .HasDefaultValueSql("0");

        builder.HasOne(x => x.Assignee)
            .WithMany()
            .HasForeignKey(x => x.AssigneeUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.AssigneeUserId);

        builder.HasOne(x => x.OwnerPosition)
            .WithMany()
            .HasForeignKey(x => x.OwnerPositionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
