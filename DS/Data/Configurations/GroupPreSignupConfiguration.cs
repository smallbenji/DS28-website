using DS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DS.Data.Configurations;

public class GroupPreSignupConfiguration : IEntityTypeConfiguration<GroupPreSignup>
{
    public void Configure(EntityTypeBuilder<GroupPreSignup> builder)
    {
        builder.ToTable("group_pre_signup", table =>
        {
            table.HasCheckConstraint("ck_group_pre_signup_beaver", "beaver >= 0");
            table.HasCheckConstraint("ck_group_pre_signup_wolf", "wolf >= 0");
            table.HasCheckConstraint("ck_group_pre_signup_junior", "junior >= 0");
            table.HasCheckConstraint("ck_group_pre_signup_trop", "trop >= 0");
            table.HasCheckConstraint("ck_group_pre_signup_senior", "senior >= 0");
            table.HasCheckConstraint("ck_group_pre_signup_rover", "rover >= 0");
            table.HasCheckConstraint("ck_group_pre_signup_leader", "leader >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Group)
            .WithOne(x => x.PreSignup)
            .HasForeignKey<GroupPreSignup>(x => x.GroupId);
        builder.HasIndex(x => x.GroupId)
            .IsUnique();

        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.GroupId)
            .IsRequired();

        builder.Property(x => x.Beaver)
            .HasDefaultValueSql("0");

        builder.Property(x => x.Wolf)
            .HasDefaultValueSql("0");

        builder.Property(x => x.Junior)
            .HasDefaultValueSql("0");

        builder.Property(x => x.Trop)
            .HasDefaultValueSql("0");

        builder.Property(x => x.Senior)
            .HasDefaultValueSql("0");

        builder.Property(x => x.Rover)
            .HasDefaultValueSql("0");

        builder.Property(x => x.Leader)
            .HasDefaultValueSql("0");
    }
}
