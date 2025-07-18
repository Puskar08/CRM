namespace CRM.Models;
public class AspNetUserRoles
{
    public string UserId { get; set; }
    public string RoleId { get; set; }

    public virtual ApplicationUser User { get; set; }
}