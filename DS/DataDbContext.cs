using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace DS;

public class DataDbContext : DbContext
{
    public DataDbContext(DbContextOptions options) : base(options) { }

    public DbSet<DS.Models.Group> Groups { get; set; }
}