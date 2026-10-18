using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class BoardColorConfiguration : IEntityTypeConfiguration<BoardColor>
{
    public void Configure(EntityTypeBuilder<BoardColor> builder)
    {
        builder.ToTable("board_color");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();
        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(7).IsRequired();
        builder.HasIndex(x => x.BoardId);
        builder.HasOne(x => x.Board)
            .WithMany(x => x.Colors)
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
