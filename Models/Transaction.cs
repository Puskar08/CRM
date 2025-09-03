using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Primitives;

namespace CRM.Models;

public class Transaction
{
    [Key]
    public int TransactionId { get; set; }
    public int Mt5LoginID { get; set; } // MT5 Login ID
    public string TransactionType { get; set; } // type of transaction Deposit/Withdrawal
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public int Status { get; set; } // Status of the transaction
    public decimal Fee { get; set; } // if any fee applied
    public string Description { get; set; } // Description of the transaction
    public string? UserId { get; set; } // who did the transaction
    // public StringValues TransactionNote { get; internal set; }
    // Additional properties can be added here if needed
}