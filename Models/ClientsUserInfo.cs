using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CRM.Models;

public class ClientsUserInfo
{
    [Key]
    public string UserId { get; set; }
    public string PassportUrl { get; set; }
    public string ProofofAddressUrl { get; set; }
    public int YearsOfTradingExperience { get; set; }
    public int IncomeLevel { get; set; }
    public int RiskToleranceLevel { get; set; }
    // Additional properties can be added here if needed
}
