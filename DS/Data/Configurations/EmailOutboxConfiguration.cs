using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutbox>
{
    public void Configure(EntityTypeBuilder<EmailOutbox> builder)
    {
        builder.ToTable("email_outbox", table =>
        {
            table.HasCheckConstraint("ck_email_outbox_attempts", "attempts >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.NextAttemptAt, x.Id });

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.EventType)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.ToEmail)
            .IsRequired();

        builder.Property(x => x.Subject)
            .IsRequired();

        builder.Property(x => x.Body)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.NextAttemptAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.Attempts)
            .IsRequired()
            .HasDefaultValueSql("0");
    }
}
