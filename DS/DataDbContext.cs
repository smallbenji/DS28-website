using DS.Models;
using Microsoft.EntityFrameworkCore;

namespace DS;

public class DataDbContext : DbContext
{
    public DataDbContext(DbContextOptions<DataDbContext> options) : base(options) { }

    public DbSet<UserInvitation> Invitations { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Patrol> Patrols { get; set; }
    public DbSet<Scout> Scouts { get; set; }
    public DbSet<PatrolMembership> PatrolMemberships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relation: Group -> Patrols
        modelBuilder.Entity<Patrol>()
            .HasOne(p => p.Group)
            .WithMany(g => g.Patrols)
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relation: Group -> Scouts
        modelBuilder.Entity<Scout>()
            .HasOne(s => s.Group)
            .WithMany(g => g.Scouts)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relation: PatrolMembership -> Patrol
        modelBuilder.Entity<PatrolMembership>()
            .HasOne(pm => pm.Patrol)
            .WithMany(p => p.Memberships)
            .HasForeignKey(pm => pm.PatrolId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relation: PatrolMembership -> Scout
        modelBuilder.Entity<PatrolMembership>()
            .HasOne(pm => pm.Scout)
            .WithMany(s => s.Memberships)
            .HasForeignKey(pm => pm.ScoutId)
            .OnDelete(DeleteBehavior.Cascade);

        // PostgreSQL Enum-mapping
        modelBuilder.Entity<Scout>()
            .Property(s => s.Gender)
            .HasConversion<string>();
    }
}