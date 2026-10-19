using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Domain.Data.Configurations;
public class WorkItemLinkConfiguration : IEntityTypeConfiguration<WorkItemLink>
{
    public void Configure(EntityTypeBuilder<WorkItemLink> builder)
    {
        builder.ToTable("work_item_link");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.WorkItemId, x.EntityType, x.EntityId })
            .IsUnique();

        builder.HasIndex(x => new { x.EntityType, x.EntityId });

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.EntityType)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne(x => x.WorkItem)
            .WithMany(x => x.Links)
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
