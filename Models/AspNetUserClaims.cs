namespace CRM.Models;
public class AspNetUserClaims
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string ClaimType { get; set; }
    public string ClaimValue { get; set; }

    public virtual ApplicationUser User { get; set; }
}