using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace CRM.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    public string Name { get; set; }
    public char? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? CountryCode { get; set; }

    // Additional properties can be added here if needed
}
