using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations;

public class WorkItemDependencyConfiguration : IEntityTypeConfiguration<WorkItemDependency>
{
    public void Configure(EntityTypeBuilder<WorkItemDependency> builder)
    {
        builder.ToTable("work_item_dependency", table => table.HasCheckConstraint("ck_work_item_dependency_not_self", "work_item_id <> depends_on_work_item_id"));
        builder.HasKey(x => new { x.WorkItemId, x.DependsOnWorkItemId });
        builder.HasIndex(x => x.DependsOnWorkItemId)
            .HasDatabaseName("ix_work_item_dependency_depends_on_work_item_id");

        builder.HasOne(x => x.WorkItem)
            .WithMany()
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DependsOnWorkItem)
            .WithMany()
            .HasForeignKey(x => x.DependsOnWorkItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
