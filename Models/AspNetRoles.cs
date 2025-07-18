namespace CRM.Models;

public class AspNetRoles
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public string ConcurrencyStamp { get; set; }

    public virtual ICollection<AspNetRoleClaims> RoleClaims { get; set; } = new List<AspNetRoleClaims>();
}