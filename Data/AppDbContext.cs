using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using CRM.Models;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    public DbSet<ClientsAccount> ClientsAccounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<ClientProfile> ClientProfiles { get; set; }
}
