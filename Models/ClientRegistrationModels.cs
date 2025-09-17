using System.ComponentModel.DataAnnotations;
namespace CRM.Models;
public class ClientRegistrationModel
{
    [Required]
    public string? CountryOfResidence { get; set; }
    [Required]
    public string? AccountType { get; set; }
    [Required]
    public string? FirstName { get; set; }
    [Required]
    public string? LastName { get; set; }
    [Required]
    public DateTime DateOfBirth { get; set; }
    [Required]
    public string? PhoneNumber { get; set; }
    [Required]
    public string? Email { get; set; }
    [Required]
    public string? Password { get; set; }
    [Required]
    public bool MarketingConsent { get; set; }
    [Required]
    public string? EmploymentStatus { get; set; }
    public string? AnnualIncome { get; set; }
    public string? PrimarySourceOfTradingFund { get; set; }
    public string? TradingObjective { get; set; }
    public string? DegreeOfRisk { get; set; }
    public int YearsOfExperience { get; set; }
    public bool ConfirmTradingKnowledge { get; set; }
    public string? BuildingNumber { get; set; }
    [Required]
    public string? Street { get; set; }
    [Required]
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    [Required]
    public string? Nationality { get; set; }
    [Required]
    public string? PlaceOfBirth { get; set; }
}

public class BasicInfoModel
{
    [Required]
    public string? CountryOfResidence { get; set; }
    [Required]
    public string? AccountType { get; set; }
    [Required]
    public string? FirstName { get; set; }
    [Required]
    public string? LastName { get; set; }
    [Required]
    public int DobYear { get; set; }
    [Required]
    public int DobMonth { get; set; }
    [Required]
    public int DobDay { get; set; }
    [Required]
    public string? PhoneCode { get; set; }
    [Required]
    public string? PhoneNumber { get; set; }
    [Required]
    public string? Email { get; set; }
    [Required]
    public string? Password { get; set; }
    [Required]
    public bool MarketingConsent { get; set; }
    
}

public class EmploymentInfoModel
{
    [Required]
    public string? EmploymentStatus { get; set; }
    public string? AnnualIncome { get; set; }
    public string? PrimarySourceOfTradingFund { get; set; }
    public string? TradingObjective { get; set; }
    public string? DegreeOfRisk { get; set; }
}

public class TradingExperienceModel
{
    public int YearsOfExperience { get; set; }
    public bool ConfirmTradingKnowledge { get; set; }
}

public class AdditionalDetailsModel
{
    public string? BuildingNumber { get; set; }
    [Required]
    public string? Street { get; set; }
    [Required]
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    [Required]
    public string? Nationality { get; set; }
    [Required]
    public string? PlaceOfBirth { get; set; }
}