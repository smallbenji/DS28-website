using DS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DS.Data;

public class DataDbContext : IdentityDbContext<User, Role, string>
{
    public DataDbContext(DbContextOptions<DataDbContext> options) : base(options) { }

    public DbSet<UserInvitation> Invitations { get; set; }
    public DbSet<RegistrationSettings> RegistrationSettings { get; set; }
    public DbSet<EmailOutbox> EmailOutbox { get; set; }

    public DbSet<Group> Groups { get; set; }
    public DbSet<Patrol> Patrols { get; set; }
    public DbSet<Scout> Scouts { get; set; }
    public DbSet<PatrolMembership> PatrolMemberships { get; set; }
    public DbSet<GroupPreSignup> GroupPreSignups { get; set; }

    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityCategory> ActivityCategories { get; set; }
    public DbSet<ActivityTeam> ActivityTeams { get; set; }
    public DbSet<ActivityTeamMembership> ActivityTeamMemberships { get; set; }
    public DbSet<ActivityTimeslot> ActivityTimeslots { get; set; }
    public DbSet<ActivityBudget> ActivityBudgets { get; set; }
    public DbSet<CatalogData> CatalogData { get; set; }

    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialOrder> MaterialOrders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataDbContext).Assembly);
    }
}
