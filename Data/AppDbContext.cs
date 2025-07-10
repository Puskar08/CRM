using Microsoft.EntityFrameworkCore;
using CRM.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Users> users { get; set; }
}
