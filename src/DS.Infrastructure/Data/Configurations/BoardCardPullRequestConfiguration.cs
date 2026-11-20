using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations;

public class BoardCardPullRequestConfiguration : IEntityTypeConfiguration<BoardCardPullRequest>
{
    public void Configure(EntityTypeBuilder<BoardCardPullRequest> builder)
    {
        builder.ToTable("board_card_pull_request");
        builder.HasKey(link => new { link.BoardCardId, link.Url });
        builder.Property(link => link.Url).HasMaxLength(512);
        builder.HasOne<BoardCard>()
            .WithMany(card => card.PullRequests)
            .HasForeignKey(link => link.BoardCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
