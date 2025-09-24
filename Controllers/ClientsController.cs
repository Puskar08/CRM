using Microsoft.AspNetCore.Mvc;
using CRM.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Transactions;
using Microsoft.AspNetCore.Routing.Tree;

namespace CRM.Controllers;

public class ClientsController : Controller
{
    private readonly ILogger<ClientsController> _logger;
    private readonly AppDbContext _context;
    private const string PendingUserSessionKey = "PendingUserId";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    public ClientsController(ILogger<ClientsController> logger, AppDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public IActionResult Index()
    {
        //var users = await _context.users.ToListAsync();
        return View();
    }

    [HttpGet]
    [Route("Clients/RegisterClient")]
    public Task<IActionResult> RegisterClient()
    {
        // Optional: Use Redis for distributed session storage
        // builder.Services.AddStackExchangeRedisCache(options =>
        // {
        //     options.Configuration = builder.Configuration.GetConnectionString("Redis");
        //     options.InstanceName = "YourApp_";
        // });
        //clear any existing session for new registration
        HttpContext.Session.Remove(PendingUserSessionKey);
        return Task.FromResult<IActionResult>(View());
    }

    // Step 1: Basic Information
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Clients/SaveBasicInfo")]
    public async Task<IActionResult> SaveBasicInfo([FromBody] BasicInfoModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid data submitted." });
        }

        // Validate DOB
        try
        {
            var dateOfBirth = new DateTime(model.DobYear, model.DobMonth, model.DobDay);
            if (dateOfBirth > DateTime.Now.AddYears(-18))
            {
                return BadRequest(new { success = false, message = "You must be at least 18 years old." });
            }
        }
        catch
        {
            return BadRequest(new { success = false, message = "Invalid date of birth." });
        }

        // Check email uniqueness
        if (string.IsNullOrEmpty(model.Email))
        {
            return BadRequest(new { success = false, message = "Email is required." });
        }
        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            return BadRequest(new { success = false, message = "Email is already registered." });
        }
        // Check password is not null
        if (string.IsNullOrEmpty(model.Password))
        {
            return BadRequest(new { success = false, message = "Password is required." });
        }
        var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create partial user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.FirstName + " " + model.LastName,
                CountryCode = model.PhoneCode,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = new DateTime(model.DobYear, model.DobMonth, model.DobDay).ToUniversalTime(),
                // MarketingConsent = model.MarketingConsent,
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new { success = false, message = $"Failed to create user: {errors}" });
            }
            //store user id in session for later steps
            HttpContext.Session.SetString(PendingUserSessionKey, user.Id);

            // Create a client profile entry
            var userProfile = new ClientProfile
            {
                UserId = user.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CountryOfResidence = model.CountryOfResidence,
                AccountType = model.AccountType,
                MarketingConsent = model.MarketingConsent,
                CreatedOn = DateTime.UtcNow
            };
            _context.ClientProfiles.Add(userProfile);
            var profileResult = await _context.SaveChangesAsync();

            if (profileResult == 0)
            {
                throw new Exception("Failed to create user profile.");
            }
            await transaction.CommitAsync();
            // Optional: Send email/SMS verification here (e.g., using SendGrid or Twilio)
            return Ok(new
            {
                success = true,
                message = "Basic information saved. Proceed to Employment Info.",
                redirectUrl = Url.Action("IncomeInfo", "Clients")
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating user or profile");
            return StatusCode(500, new { success = false, message = "An error occurred while processing your request. Please try again." });
        }

    }

    [HttpGet]
    [Route("Clients/IncomeInfo")]
    public async Task<IActionResult> IncomeInfo()
    {
        var pendingUserId = HttpContext.Session.GetString(PendingUserSessionKey);
        if (string.IsNullOrEmpty(pendingUserId))
        {
            return RedirectToAction("RegisterClient");
        }

        var user = await _userManager.FindByIdAsync(pendingUserId);
        var profile = await _context.ClientProfiles.FindAsync(pendingUserId);
        if (string.IsNullOrEmpty(pendingUserId) || user == null || profile == null)
        {
            return RedirectToAction("RegisterClient");
        }

        var model = new EmploymentInfoModel
        {
            EmploymentStatus = profile.EmploymentStatus,
            AnnualIncome = profile.AnnualIncome,
            PrimarySourceOfTradingFund = profile.PrimarySourceOfTradingFund,
            TradingObjective = profile.TradingObjective,
            DegreeOfRisk = profile.DegreeOfRisk
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Clients/SaveIncomeInfo")]
    public async Task<IActionResult> SaveIncomeInfo([FromBody] EmploymentInfoModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid data submitted." });
        }

        var pendingUserId = HttpContext.Session.GetString(PendingUserSessionKey);
        if (string.IsNullOrEmpty(pendingUserId))
        {
            return BadRequest(new { success = false, message = "Session expired. Please restart registration." });
        }

        var user = await _userManager.FindByIdAsync(pendingUserId);
        var profile = await _context.ClientProfiles.FindAsync(pendingUserId);

        if (user == null || profile == null)
        {
            return BadRequest(new { success = false, message = "Profile not found. Please restart registration." });
        }
        var saveIncomeTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Update profile with trading experience info
            profile.EmploymentStatus = model.EmploymentStatus;
            profile.AnnualIncome = model.AnnualIncome;
            profile.PrimarySourceOfTradingFund = model.PrimarySourceOfTradingFund;
            profile.TradingObjective = model.TradingObjective;
            profile.DegreeOfRisk = model.DegreeOfRisk;

            _context.ClientProfiles.Update(profile);
            var result = await _context.SaveChangesAsync();

            if (result == 0)
            {
                throw new Exception("Failed to create user profile.");
            }

            await saveIncomeTransaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = "Employment and income information saved.",
                redirectUrl = Url.Action("TradingInfo", "Clients") // Redirect to home or dashboard
            });
        }
        catch (Exception ex)
        {
            await saveIncomeTransaction.RollbackAsync();
            _logger.LogError(ex, "Error saving income info for user {UserId}", pendingUserId);
            return StatusCode(500, new { success = false, message = "An error occurred while saving income information. Please try again." });
        }
    }

    [HttpGet]
    [Route("Clients/TradingInfo")]
    public async Task<IActionResult> TradingInfo()
    {
        var pendingUserId = HttpContext.Session.GetString(PendingUserSessionKey);
        if (string.IsNullOrEmpty(pendingUserId))
        {
            return RedirectToAction("RegisterClient");
        }

        var user = await _userManager.FindByIdAsync(pendingUserId);
        var profile = await _context.ClientProfiles.FindAsync(pendingUserId);
        if (string.IsNullOrEmpty(pendingUserId) || user == null || profile == null)
        {
            return RedirectToAction("RegisterClient");
        }

        var model = new TradingExperienceModel
        {
            YearsOfExperience = profile.YearsOfTradingExperience,
            ConfirmTradingKnowledge = profile.ConfirmTradingKnowledge,
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Clients/SaveTradingInfo")]
    public async Task<IActionResult> SaveTradingInfo([FromBody] TradingExperienceModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid data submitted." });
        }

        var pendingUserId = HttpContext.Session.GetString(PendingUserSessionKey);
        if (string.IsNullOrEmpty(pendingUserId))
        {
            return BadRequest(new { success = false, message = "Session expired. Please restart registration." });
        }

        var user = await _userManager.FindByIdAsync(pendingUserId);
        var profile = await _context.ClientProfiles.FindAsync(pendingUserId);

        if (user == null || profile == null)
        {
            return BadRequest(new { success = false, message = "Profile not found. Please restart registration." });
        }

        var tradingInfoTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Update profile with trading experience info
            profile.YearsOfTradingExperience = model.YearsOfExperience;
            profile.ConfirmTradingKnowledge = model.ConfirmTradingKnowledge;
            _context.ClientProfiles.Update(profile);
            var result = await _context.SaveChangesAsync();

            if (result == 0)
            {
                throw new Exception("Failed to create user profile.");
            }

            await tradingInfoTransaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = "Trading experience information saved. Registration complete.",
                redirectUrl = Url.Action("AdditionalDetails", "Clients") // Redirect to home or dashboard
            });
        }
        catch (Exception ex)
        {
            await tradingInfoTransaction.RollbackAsync();
            _logger.LogError(ex, "Error saving trading info for user {UserId}", pendingUserId);
            return StatusCode(500, new { success = false, message = "An error occurred while saving trading information. Please try again." });
        }
    }

    [HttpGet]
    [Route("Clients/AdditionalDetails")]
    public async Task<IActionResult> AdditionalDetails()
    {
        var pendingUserId = HttpContext.Session.GetString(PendingUserSessionKey);
        if (string.IsNullOrEmpty(pendingUserId))
        {
            return RedirectToAction("RegisterClient");
        }

        var user = await _userManager.FindByIdAsync(pendingUserId);
        var profile = await _context.ClientProfiles.FindAsync(pendingUserId);
        if (user == null || profile == null)
        {
            return RedirectToAction("RegisterClient");
        }

        var model = new AdditionalDetailsModel
        {
            BuildingNumber = profile.BuildingNumber,
            Street = profile.Street,
            City = profile.City,
            PostalCode = profile.PostalCode,
            Nationality = profile.Nationality,
            PlaceOfBirth = profile.PlaceOfBirth
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Clients/SaveAdditionalDetails")]
    public async Task<IActionResult> SaveAdditionalDetails([FromBody] AdditionalDetailsModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid data submitted." });
        }

        var pendingUserId = HttpContext.Session.GetString(PendingUserSessionKey);
        if (string.IsNullOrEmpty(pendingUserId))
        {
            return BadRequest(new { success = false, message = "Session expired. Please restart registration." });
        }

        var user = await _userManager.FindByIdAsync(pendingUserId);
        var profile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == pendingUserId);

        if (user == null || profile == null)
        {
            return BadRequest(new { success = false, message = "Profile not found. Please restart registration." });
        }

        var additionalDetailsTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Update profile with additional details
            profile.BuildingNumber = model.BuildingNumber;
            profile.Street = model.Street;
            profile.City = model.City;
            profile.PostalCode = model.PostalCode;
            profile.Nationality = model.Nationality;
            profile.PlaceOfBirth = model.PlaceOfBirth;
            _context.ClientProfiles.Update(profile);
            var result = await _context.SaveChangesAsync();
            if (result == 0)
            {
                throw new Exception("Failed to create user profile.");
            }
            await additionalDetailsTransaction.CommitAsync();

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: false); // Non-persistent session
            // Clear session after successful registration
            HttpContext.Session.Remove(PendingUserSessionKey);

            return Ok(new
            {
                success = true,
                message = "Additional details saved. Registration complete.",
                redirectUrl = Url.Action("CompleteProfile", "Clients") // Redirect to complete profile
            });

        }
        catch (Exception ex)
        {
            await additionalDetailsTransaction.RollbackAsync();
            _logger.LogError(ex, "Error saving additional details for user {UserId}", pendingUserId);
            return StatusCode(500, new { success = false, message = "An error occurred while saving additional details. Please try again." });
        }
    }

    [HttpGet]
    // [Authorize]
    [Route("Clients/CompleteProfile")]
    public async Task<IActionResult> CompleteProfile()
    {
        var user = await _userManager.FindByIdAsync("67070c44-88c3-4a7e-8e1b-4fbc3e078b56");
        var profile = _context.ClientProfiles.FirstOrDefault(p => p.UserId == "67070c44-88c3-4a7e-8e1b-4fbc3e078b56");
        var model = new ProfileViewModel
        {
            UserId = profile.UserId,
            CountryOfResidence = profile.CountryOfResidence,
            AccountType = profile.AccountType,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Email = user.Email,
            DateOfBirth = user.DateOfBirth,
            PhoneCode = user.CountryCode,
            PhoneNumber = user.PhoneNumber,
            Gender = profile.Gender,
            MarketingConsent = profile.MarketingConsent,
            EmploymentStatus = profile.EmploymentStatus,
            AnnualIncome = profile.AnnualIncome,
            PrimarySourceOfTradingFund = profile.PrimarySourceOfTradingFund,
            TradingObjective = profile.TradingObjective,
            DegreeOfRisk = profile.DegreeOfRisk,
            YearsOfTradingExperience = profile.YearsOfTradingExperience,
            ConfirmTradingKnowledge = profile.ConfirmTradingKnowledge,
            BuildingNumber = profile.BuildingNumber,
            Street = profile.Street,
            City = profile.City,
            PostalCode = profile.PostalCode,
            Nationality = profile.Nationality,
            PlaceOfBirth = profile.PlaceOfBirth,
            PassportUrl = profile.PassportUrl,
            ProofofAddressUrl = profile.ProofofAddressUrl,
            IsProfileComplete = profile.IsProfileComplete,
            IsAcceptTermsandConditions = profile.IsAcceptTermsandConditions
        };
        // var user = await _userManager.GetUserAsync(User);
        // if(user == null)
        // {
        //     return RedirectToAction("Login", "Account");
        // }
        // var profile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        // if(profile == null)
        // {
        //     return RedirectToAction("RegisterClient", "Clients");
        // }
        return View("ProfileComplete", model);
    }

    public static List<string> ValidateRequiredFields<T>(T obj, params string[] requiredFields)
    {
        var missingFields = new List<string>();

        if (obj == null)
        {
            missingFields.Add("Object is null");
            return missingFields;
        }

        var type = typeof(T);

        foreach (var field in requiredFields)
        {
            var prop = type.GetProperty(field);
            if (prop != null)
            {
                var value = prop.GetValue(obj)?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    missingFields.Add(field);
                }
            }
        }

        return missingFields; // empty list means all fields are filled
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    // [Authorize]
    [Route("Clients/UpdateProfileData")]
    public async Task<IActionResult> UpdateProfileData([FromBody] CRM.Models.UpdateProfileDTO profile)
    {
        string successMessage = string.Empty;
        List<string> missingFields = new List<string>();
        var redirectUrl = "";
        try
        {
            if (profile != null)
            {
                var clientProfile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == profile.ProfileData.UserId);
                ApplicationUser? user = null;
                if (!string.IsNullOrEmpty(profile.ProfileData.UserId))
                {
                    user = await _userManager.FindByIdAsync(profile.ProfileData.UserId);
                }
                if (clientProfile == null || user == null)
                {
                    return StatusCode(500, new { success = false, message = "Profile not found." });
                }
                var updateProfileDataTransaction = await _context.Database.BeginTransactionAsync();
                switch (profile.ProfileSection)
                {
                    case "PersonalDetails":
                        missingFields = ValidateRequiredFields(profile.ProfileData,
                           nameof(profile.ProfileData.UserId),
                           nameof(profile.ProfileData.FirstName),
                           nameof(profile.ProfileData.LastName),
                           nameof(profile.ProfileData.Email),
                           nameof(profile.ProfileData.PhoneCode),
                           nameof(profile.ProfileData.PhoneNumber),
                           nameof(profile.ProfileData.AccountType),
                           nameof(profile.ProfileData.DateOfBirth),
                           nameof(profile.ProfileData.CountryOfResidence)
                       );

                        if (missingFields.Any())
                        {
                            // You can return all missing fields in a message
                            var message = "The following fields are required: " + string.Join(", ", missingFields);
                            return BadRequest(new { success = false, message = message });
                        }

                        //update client profile fields
                        clientProfile.FirstName = profile.ProfileData.FirstName;
                        clientProfile.LastName = profile.ProfileData.LastName;
                        clientProfile.AccountType = profile.ProfileData.AccountType;
                        clientProfile.CountryOfResidence = profile.ProfileData.CountryOfResidence;

                        //update client user fields
                        user.Name = profile.ProfileData.FirstName + " " + profile.ProfileData.LastName;
                        user.CountryCode = profile.ProfileData.PhoneCode;
                        user.PhoneNumber = profile.ProfileData.PhoneNumber;
                        user.DateOfBirth = profile.ProfileData.DateOfBirth?.ToUniversalTime();

                        var result = await _userManager.UpdateAsync(user);
                        if (!result.Succeeded)
                        {
                            return StatusCode(500, new { success = false, message = "Unable to update client user." });
                        }

                        successMessage = "Personal details updated successfully.";
                        break;

                    case "Employment&Income":
                        missingFields = ValidateRequiredFields(profile.ProfileData,
                            nameof(profile.ProfileData.EmploymentStatus),
                            nameof(profile.ProfileData.AnnualIncome),
                            nameof(profile.ProfileData.PrimarySourceOfTradingFund),
                            nameof(profile.ProfileData.TradingObjective),
                            nameof(profile.ProfileData.DegreeOfRisk)
                        );

                        if (missingFields.Any())
                        {
                            successMessage = "The following fields are required: " + string.Join(", ", missingFields);
                            return BadRequest(new { success = false, message = successMessage });
                        }
                        if (clientProfile != null)
                        {
                            clientProfile.EmploymentStatus = profile.ProfileData.EmploymentStatus;
                            clientProfile.AnnualIncome = profile.ProfileData.AnnualIncome;
                            clientProfile.PrimarySourceOfTradingFund = profile.ProfileData.PrimarySourceOfTradingFund;
                            clientProfile.TradingObjective = profile.ProfileData.TradingObjective;
                            clientProfile.DegreeOfRisk = profile.ProfileData.DegreeOfRisk;
                        }
                        successMessage = "Employment and income details updated successfully.";
                        break;

                    case "TradingPreference":
                        missingFields = ValidateRequiredFields(profile.ProfileData,
                            nameof(profile.ProfileData.YearsOfTradingExperience),
                            nameof(profile.ProfileData.ConfirmTradingKnowledge)
                        );

                        if (missingFields.Any())
                        {
                            successMessage = "The following fields are required: " + string.Join(", ", missingFields);
                            return BadRequest(new { success = false, message = successMessage });
                        }
                        if (clientProfile != null)
                        {
                            clientProfile.YearsOfTradingExperience = profile.ProfileData.YearsOfTradingExperience;
                            clientProfile.ConfirmTradingKnowledge = profile.ProfileData.ConfirmTradingKnowledge;
                        }
                        successMessage = "Trading preferences updated successfully.";
                        break;

                    case "AdditionalDetails":
                        missingFields = ValidateRequiredFields(profile.ProfileData,
                            nameof(profile.ProfileData.Street),
                            nameof(profile.ProfileData.City),
                            nameof(profile.ProfileData.Nationality),
                            nameof(profile.ProfileData.PlaceOfBirth)
                        );

                        if (missingFields.Any())
                        {
                            successMessage = "The following fields are required: " + string.Join(", ", missingFields);
                            return BadRequest(new { success = false, message = successMessage });
                        }
                        if (clientProfile != null)
                        {
                            clientProfile.BuildingNumber = profile.ProfileData.BuildingNumber;
                            clientProfile.Street = profile.ProfileData.Street;
                            clientProfile.PostalCode = profile.ProfileData.PostalCode;
                            clientProfile.Nationality = profile.ProfileData.Nationality;
                            clientProfile.PlaceOfBirth = profile.ProfileData.PlaceOfBirth;
                        }
                        successMessage = "Additional details updated successfully.";
                        break;

                    case "Declaration":
                        missingFields = ValidateRequiredFields(profile.ProfileData,
                            nameof(profile.ProfileData.IsAcceptTermsandConditions)
                        );

                        if (missingFields.Any())
                        {
                            successMessage = "The following fields are required: " + string.Join(", ", missingFields);
                            return BadRequest(new { success = false, message = successMessage });
                        }
                        if (clientProfile != null)
                        {
                            clientProfile.IsAcceptTermsandConditions = profile.ProfileData.IsAcceptTermsandConditions;
                        }
                        successMessage = "Declartion updated successfully.";
                        redirectUrl = Url.Action("VerifyProfile", "Clients");
                        break;

                    case "Verification":
                        break;
                    default:
                        break;
                }
                if (clientProfile != null)
                {
                    _context.ClientProfiles.Update(clientProfile);

                    var savechangesResult = await _context.SaveChangesAsync();
                    if (savechangesResult > 0)
                    {
                        await updateProfileDataTransaction.CommitAsync();
                        return Ok(new { success = true, message = successMessage, updatedRecord = clientProfile, redirectUrl = redirectUrl });
                    }
                }
            }
            return BadRequest(new { success = false, message = "Invalid form data." });
        }
        catch (Exception ex)
        {
            successMessage = ex.Message;
            return BadRequest(new { success = false, message = "An error occurred while updating the profile." });
        }
    }

    [HttpGet]
    [Route("Clients/VerifyProfile")]
    public async Task<IActionResult> VerifyProfile()
    {
        return View();
    }

}
