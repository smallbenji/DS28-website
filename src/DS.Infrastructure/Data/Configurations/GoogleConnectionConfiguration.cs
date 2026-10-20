using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Infrastructure.Data.Configurations;
public class GoogleConnectionConfiguration : IEntityTypeConfiguration<GoogleConnection>
{
    public void Configure(EntityTypeBuilder<GoogleConnection> builder)
    {
        builder.ToTable("google_connection");

        builder.Property(c => c.AccountEmail)
            .IsRequired();

        builder.Property(c => c.RefreshToken)
            .IsRequired();

        builder.HasOne(c => c.ConnectedBy)
            .WithMany()
            .HasForeignKey(c => c.ConnectedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
