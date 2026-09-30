using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class IdentityUserPasskeyConfiguration : IEntityTypeConfiguration<IdentityUserPasskey<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserPasskey<string>> builder)
    {
        builder.ToTable("asp_net_user_passkeys");

        builder.OwnsOne(x => x.Data).ToJson("data");

        builder.Property(x => x.UserId)
            .IsRequired();
    }
}
