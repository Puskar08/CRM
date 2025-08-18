using Microsoft.AspNetCore.Identity;
namespace CRM.Models;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; }
    // Additional properties can be added here if needed
}
