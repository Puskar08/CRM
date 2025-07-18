namespace CRM.Models;
public class AspNetUsers
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string NormalizedUserName { get; set; }
    public string Email { get; set; }
    public string NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string PasswordHash { get; set; }
    public string SecurityStamp { get; set; }
    public string ConcurrencyStamp { get; set; }
    public string PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }

    // Additional properties can be added here if needed//

    public virtual ICollection<AspNetUserRoles> UserRoles { get; set; }
    public virtual ICollection<AspNetUserLogins> UserLogins { get; set; }
    public virtual ICollection<AspNetUserClaims> UserClaims { get; set; }
}