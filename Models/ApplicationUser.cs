using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace CRM.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    public string Name { get; set; }
    [Required]
    public char? Gender { get; set; }
    [Required]
    public DateTime DateOfBirth { get; set; }
    [Required]
    public string Nationality { get; set; } = string.Empty;

    // Additional properties can be added here if needed
}
