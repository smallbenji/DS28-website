using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations
{
    public class BoardTagConfiguration : IEntityTypeConfiguration<BoardTag>
    {
        public void Configure(EntityTypeBuilder<BoardTag> builder)
        {
            builder.ToTable("board_tag");
            builder.HasKey(tag => tag.Id);
            builder.HasAlternateKey(tag => new { tag.BoardId, tag.Id });
            builder.Property(tag => tag.Id).UseIdentityAlwaysColumn();
            builder.Property(tag => tag.Name).HasMaxLength(64).IsRequired();
            builder.Property(tag => tag.Color).HasMaxLength(7);
            builder.HasOne(tag => tag.Board).WithMany(board => board.Tags)
                .HasForeignKey(tag => tag.BoardId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
