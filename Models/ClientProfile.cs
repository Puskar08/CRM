using System.ComponentModel.DataAnnotations;

namespace CRM.Models
{

    public class ClientProfile
    {
        [Key]
        public string? UserId { get; set; }
        // Additional properties can be added here if needed
        public string? CountryOfResidence { get; set; }
        public string? AccountType { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public bool MarketingConsent { get; set; }//
        public string? EmploymentStatus { get; set; }
        public string? AnnualIncome { get; set; }
        public string? PrimarySourceOfTradingFund { get; set; }
        public string? TradingObjective { get; set; }
        public string? DegreeOfRisk { get; set; }
        public string? YearsOfTradingExperience { get; set; }
        public bool ConfirmTradingKnowledge { get; set; }
        public string? BuildingNumber { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Nationality { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? PassportUrl { get; set; }
        public string? ProofofAddressUrl { get; set; }
        public bool IsProfileComplete { get; set; }
        public DateTime CreatedOn { get; set; }
    }


    public class ProfileViewModel
    {
        public string? UserId { get; set; }
        // Additional properties can be added here if needed
        public string? CountryOfResidence { get; set; }
        public string? AccountType { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneCode { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public bool MarketingConsent { get; set; }//
        public string? EmploymentStatus { get; set; }
        public string? AnnualIncome { get; set; }
        public string? PrimarySourceOfTradingFund { get; set; }
        public string? TradingObjective { get; set; }
        public string? DegreeOfRisk { get; set; }
        public string? YearsOfTradingExperience { get; set; }
        public bool ConfirmTradingKnowledge { get; set; }
        public string? BuildingNumber { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Nationality { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? PassportUrl { get; set; }
        public string? ProofofAddressUrl { get; set; }
        public bool IsProfileComplete { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}