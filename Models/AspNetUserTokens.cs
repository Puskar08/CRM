namespace CRM.Models;
public class AspnetUserTokens
{
    public string UserId { get; set; }
    public string LoginProvider { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }

    public virtual ApplicationUser User { get; set; }
}