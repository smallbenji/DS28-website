using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class PatrolConfiguration : IEntityTypeConfiguration<Patrol>
{
    public void Configure(EntityTypeBuilder<Patrol> builder)
    {
        builder.ToTable("patrol");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Patrols)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Name)
            .IsRequired();

        builder.Property(x => x.GroupId)
            .IsRequired();
    }
}
