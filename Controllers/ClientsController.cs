using Microsoft.AspNetCore.Mvc;
using CRM.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers;

public class ClientsController : Controller
{
    private readonly ILogger<ClientsController> _logger;
    private readonly AppDbContext _context;
    private const string PendingUserSessionKey = "PendingUserId";
    private readonly UserManager<ApplicationUser> _userManager;
    public ClientsController(ILogger<ClientsController> logger, AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
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
                PhoneNumber = $"{model.PhoneCode}{model.PhoneNumber}",
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


    // [HttpGet]
    // [Route("Clients/EmploymentInfo")]
    // public async Task<IActionResult> EmploymentInfo()
    // {
    //     var PendingUserId = HttpContext.Session.GetString(PendingUserSessionKey);
    //     if (string.IsNullOrEmpty(PendingUserId))
    //     {
    //         return RedirectToAction("RegisterClient");
    //     }
    //     var user = await _userManager.FindByIdAsync(PendingUserId);
    //     if (user == null)
    //     {
    //         return RedirectToAction("RegisterClient");
    //     }
    //     var profile = await _context.ClientProfiles.FindAsync(PendingUserId);
    //     if (profile == null)
    //     {
    //         return RedirectToAction("RegisterClient");
    //     }
    //     var model = new EmploymentInfoModel
    //     {
    //         EmploymentStatus = profile.EmploymentStatus,
    //         AnnualIncome = profile.AnnualIncome,
    //         PrimarySourceOfTradingFund = profile.PrimarySourceOfTradingFund,
    //         TradingObjective = profile.TradingObjective,
    //         DegreeOfRisk = profile.DegreeOfRisk
    //     };
    //     return View(model);
    // }

    // [HttpPost]
    // [ValidateAntiForgeryToken]
    // [Route("Clients/SaveEmploymentInfo")]
    // public async Task<IActionResult> SaveEmploymentInfo([FromBody] EmploymentInfoModel model)
    // {
    //     if (!ModelState.IsValid)
    //     {
    //         return BadRequest(new { success = false, message = "Invalid data submitted." });
    //     }

    //     var PendingUserId = HttpContext.Session.GetString(PendingUserSessionKey);
    //     if (string.IsNullOrEmpty(PendingUserId))
    //     {
    //         return BadRequest(new { success = false, message = "Session expired. Please restart registration." });
    //     }

    //     var user = await _userManager.FindByIdAsync(PendingUserId);
    //     var profile = await _context.ClientProfiles.FindAsync(PendingUserId);

    //     if (user == null || profile == null)
    //     {
    //         return BadRequest(new { success = false, message = "Profile not found. Please restart registration." });
    //     }

    //     // Update profile with employment info
    //     profile.EmploymentStatus = model.EmploymentStatus;
    //     profile.AnnualIncome = model.AnnualIncome;
    //     profile.PrimarySourceOfTradingFund = model.PrimarySourceOfTradingFund;
    //     profile.TradingObjective = model.TradingObjective;
    //     profile.DegreeOfRisk = model.DegreeOfRisk;

    //     _context.ClientProfiles.Update(profile);
    //     var result = await _context.SaveChangesAsync();

    //     return Ok(new
    //     {
    //         success = true,
    //         message = "Basic information saved. Proceed to Employment Info.",
    //         redirectUrl = Url.Action("TradingInfo", "Clients")
    //     });
    // }

    // [HttpPost]
    // [ValidateAntiForgeryToken]
    // [Route("Clients/SaveEmploymentInfo1")]
    // public async Task<IActionResult> SaveEmploymentInfo1([FromBody] EmploymentInfoModel model)
    // {
    //     if (!ModelState.IsValid)
    //     {
    //         return BadRequest(new
    //         {
    //             success = false,
    //             message = "Invalid data submitted.",
    //             errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
    //         });
    //     }

    //     var pendingUserId = HttpContext.Session.GetString(PendingUserSessionKey);
    //     if (string.IsNullOrEmpty(pendingUserId))
    //     {
    //         return BadRequest(new
    //         {
    //             success = false,
    //             message = "Session expired. Please restart registration.",
    //             redirectUrl = Url.Action("RegisterClient", "Clients")
    //         });
    //     }

    //     var user = await _userManager.FindByIdAsync(pendingUserId);
    //     if (user == null)
    //     {
    //         HttpContext.Session.Remove(PendingUserSessionKey);
    //         return BadRequest(new
    //         {
    //             success = false,
    //             message = "User not found or registration already completed.",
    //             redirectUrl = Url.Action("RegisterClient", "Clients")
    //         });
    //     }

    //     // Query ClientProfile by UserId, not primary key
    //     var profile = await _context.ClientProfiles.FirstOrDefaultAsync(cp => cp.UserId == pendingUserId);
    //     if (profile == null || profile.IsProfileComplete)
    //     {
    //         HttpContext.Session.Remove(PendingUserSessionKey);
    //         return BadRequest(new
    //         {
    //             success = false,
    //             message = "Profile not found. Please restart registration.",
    //             redirectUrl = Url.Action("RegisterClient", "Clients")
    //         });
    //     }

    //     using var transaction = await _context.Database.BeginTransactionAsync();
    //     try
    //     {
    //         // Update profile
    //         profile.EmploymentStatus = model.EmploymentStatus;
    //         profile.AnnualIncome = model.AnnualIncome;
    //         profile.PrimarySourceOfTradingFund = model.PrimarySourceOfTradingFund;
    //         profile.TradingObjective = model.TradingObjective;
    //         profile.DegreeOfRisk = model.DegreeOfRisk;

    //         _context.ClientProfiles.Update(profile);
    //         var result = await _context.SaveChangesAsync();
    //         if (result == 0)
    //         {
    //             throw new Exception("No changes were saved to the database.");
    //         }

    //         await transaction.CommitAsync();

    //         return Ok(new
    //         {
    //             success = true,
    //             message = "Employment information saved successfully.",
    //             redirectUrl = Url.Action("TradingInfo", "Clients")
    //         });
    //     }
    //     catch (Exception ex)
    //     {
    //         await transaction.RollbackAsync();
    //         _logger.LogError(ex, "Error saving employment info for user {UserId}", pendingUserId);
    //         return StatusCode(500, new
    //         {
    //             success = false,
    //             message = "An error occurred while saving employment information. Please try again."
    //         });
    //     }
    // }

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

        // // Update profile with trading experience info
        // profile.EmploymentStatus = model.EmploymentStatus;
        // profile.AnnualIncome = model.AnnualIncome;
        // profile.PrimarySourceOfTradingFund = model.PrimarySourceOfTradingFund;
        // profile.TradingObjective = model.TradingObjective;
        // profile.DegreeOfRisk = model.DegreeOfRisk;

        // _context.ClientProfiles.Update(profile);
        // var result = await _context.SaveChangesAsync();

        // if (result > 0)
        // {

        //     return Ok(new
        //     {
        //         success = true,
        //         message = "Trading experience information saved. Registration complete.",
        //         redirectUrl = Url.Action("TradingInfo", "Clients") // Redirect to home or dashboard
        //     });
        // }

        // return BadRequest(new { success = false, message = "Failed to save trading experience information." });
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
        // Update profile with trading experience info
        // profile.YearsOfTradingExperience = model.YearsOfExperience;
        // profile.ConfirmTradingKnowledge = model.ConfirmTradingKnowledge;
        // _context.ClientProfiles.Update(profile);
        // var result = await _context.SaveChangesAsync();

        // if (result > 0)
        // {

        //     return Ok(new
        //     {
        //         success = true,
        //         message = "Trading experience information saved. Registration complete.",
        //         redirectUrl = Url.Action("AdditionalDetails", "Clients") // Redirect to home or dashboard
        //     });
        // }

        // return BadRequest(new { success = false, message = "Failed to save trading experience information." });
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
        var profile = await _context.ClientProfiles.FindAsync(pendingUserId);

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

            // Clear session after successful registration
            HttpContext.Session.Remove(PendingUserSessionKey);

            return Ok(new
            {
                success = true,
                message = "Additional details saved. Registration complete.",
                redirectUrl = Url.Action("Index", "Home") // Redirect to home or dashboard
            });

        }
        catch (Exception ex)
        {
            await additionalDetailsTransaction.RollbackAsync();
            _logger.LogError(ex, "Error saving additional details for user {UserId}", pendingUserId);
            return StatusCode(500, new { success = false, message = "An error occurred while saving additional details. Please try again." });
        }
    }
}
