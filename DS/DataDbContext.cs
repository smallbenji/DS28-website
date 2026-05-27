using Microsoft.EntityFrameworkCore;

namespace DS;

public class DataDbContext : DbContext
{
    public DataDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Models.Group> Groups { get; set; }
    public DbSet<Models.UserInvitation> Invitations { get; set; }
}