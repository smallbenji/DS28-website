using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Domain.Data.Configurations;
public class BoardCardConfiguration : IEntityTypeConfiguration<BoardCard>
{
    public void Configure(EntityTypeBuilder<BoardCard> builder)
    {
        builder.ToTable("board_card");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.WorkItemId, x.BoardId })
            .IsUnique();

        builder.HasIndex(x => x.BoardId);

        builder.HasIndex(x => x.ColumnId);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.HasOne(x => x.Board)
            .WithMany()
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Column)
            .WithMany()
            .HasForeignKey(x => x.ColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkItem)
            .WithMany()
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
