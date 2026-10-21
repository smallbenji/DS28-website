using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations;

public class WorkItemAssigneeConfiguration : IEntityTypeConfiguration<WorkItemAssignee>
{
    public void Configure(EntityTypeBuilder<WorkItemAssignee> builder)
    {
        builder.ToTable("work_item_assignee");
        builder.HasKey(x => new { x.WorkItemId, x.UserId });
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_work_item_assignees_user_id");

        builder.HasOne(x => x.WorkItem)
            .WithMany(x => x.Assignees)
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
