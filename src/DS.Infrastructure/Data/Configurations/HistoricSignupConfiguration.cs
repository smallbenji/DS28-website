using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations
{
    public class HistoricSignupConfiguration : IEntityTypeConfiguration<HistoricSignup>
    {
        public void Configure(EntityTypeBuilder<HistoricSignup> builder)
        {
            builder.ToTable("historic_signup");
            builder.HasKey(x => new { x.GroupId, x.CampYear, x.Category, x.DayIndex });
            builder.HasIndex(x => new { x.CampYear, x.SourceDocumentId, x.Category, x.DayIndex }).IsUnique();
            builder.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
            builder.Property(x => x.Category).HasMaxLength(16).IsRequired();
            builder.Property(x => x.SourceDocumentId).IsRequired();
            builder.Property(x => x.SourceSha256).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ImportedAt).IsRequired();
        }
    }
}
