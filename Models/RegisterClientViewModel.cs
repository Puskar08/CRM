using System.ComponentModel.DataAnnotations;
public class RegisterClientViewModel
{
    public int Id { get; set; }

    // Step 1: Personal Information
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required]
    public string Gender { get; set; }

    [Required]
    public string Nationality { get; set; }

    [Required]
    [Phone]
    public string Phone { get; set; }

    // Step 2: Trading Information
    [Required]
    public string Experience { get; set; }

    [Required]
    public string Investment { get; set; }

    public string PreferredMarket { get; set; }
    public string RiskTolerance { get; set; }

    // Step 3: Documents
    public string DocumentPaths { get; set; } // Comma-separated paths
    public string Notes { get; set; }

    public DateTime CreatedDate { get; set; }
}