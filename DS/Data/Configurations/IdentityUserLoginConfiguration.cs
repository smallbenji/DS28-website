using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> builder)
    {
        builder.ToTable("asp_net_user_logins");

        builder.Property(x => x.LoginProvider)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ProviderKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();
    }
}
