using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Domain.Data.Configurations;
public class BoardColumnConfiguration : IEntityTypeConfiguration<BoardColumn>
{
    public void Configure(EntityTypeBuilder<BoardColumn> builder)
    {
        builder.ToTable("board_column", table =>
        {
            table.HasCheckConstraint("ck_board_column_wip_limit", "wip_limit >= 1");
        });

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.BoardId, x.Position })
            .IsUnique();

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.IsDoneColumn)
            .IsRequired()
            .HasDefaultValueSql("FALSE")
            .HasSentinel(false);

        builder.HasOne(x => x.Board)
            .WithMany(x => x.Columns)
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
