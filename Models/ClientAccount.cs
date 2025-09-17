using System.ComponentModel.DataAnnotations;

namespace CRM.Models;

public class ClientAccount
{
    [Key]
    public int Id { get; set; }
    public string UserId { get; set; }
    public int Mt5LoginID { get; set; }
    public string AccountType { get; set; }
    public DateTime CreatedDate { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Balance { get; set; }
    public decimal CreditBalance { get; set; }

    // Additional properties can be added here if needed
}