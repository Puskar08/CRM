using System.ComponentModel.DataAnnotations;

namespace CRM.Models;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
    [DataType(DataType.Password)]
    [Compare("Password")]
    public string? ConfirmPassword { get; set; }
    [Required]
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
}
public class AccountViewModel
{
    [Required]
    public string? AccountType { get; set; }
    [Required]
    public string? Leverage { get; set; }
    [Required]
    public string? Currency { get; set; }
    [Required]
    public string? Platform { get; set; }
}