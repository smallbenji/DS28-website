using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("asp_net_roles");

        builder.Property(x => x.Name)
            .HasMaxLength(256);

        builder.Property(x => x.NormalizedName)
            .HasMaxLength(256);
    }
}
