using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class ScoutConfiguration : IEntityTypeConfiguration<Scout>
{
    public void Configure(EntityTypeBuilder<Scout> builder)
    {
        builder.ToTable("scout", table =>
        {
            table.HasCheckConstraint("ck_scout_gender", "gender IN ('MALE', 'FEMALE')");
        });

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Scouts)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Name)
            .IsRequired();

        builder.Property(x => x.Birthday)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasConversion(value => value.ToString().ToUpperInvariant(), value => Enum.Parse<Gender>(value, true))
            .IsRequired();

        builder.Property(x => x.GroupId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
