using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations
{
    public class BoardCardTagConfiguration : IEntityTypeConfiguration<BoardCardTag>
    {
        public void Configure(EntityTypeBuilder<BoardCardTag> builder)
        {
            builder.ToTable("board_card_tag");
            builder.HasKey(link => new { link.BoardCardId, link.BoardTagId });
            builder.HasOne(link => link.BoardCard).WithMany(card => card.Tags)
                .HasForeignKey(link => new { link.BoardId, link.BoardCardId })
                .HasPrincipalKey(card => new { card.BoardId, card.Id })
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(link => link.BoardTag).WithMany()
                .HasForeignKey(link => new { link.BoardId, link.BoardTagId })
                .HasPrincipalKey(tag => new { tag.BoardId, tag.Id })
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
