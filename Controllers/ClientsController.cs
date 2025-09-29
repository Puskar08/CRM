using Microsoft.AspNetCore.Mvc;
using CRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CRM.Controllers;

public class ClientsController : Controller
{
    private readonly ILogger<ClientsController> _logger;
    private readonly AppDbContext _context;
    private const string PendingUserSessionKey = "PendingUserId";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
    public ClientsController(ILogger<ClientsController> logger, AppDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public IActionResult Index()
    {
        //var users = await _context.users.ToListAsync();
        return View();
    }

    // Helper: Redirect based on registration step
    private IActionResult RedirectToRegistrationStep(int step, string? targetUserId)
    {
        switch (step)
        {
            case 1:
                return RedirectToAction("IncomeInfo", targetUserId);
            case 2:
                return RedirectToAction("TradingInfo", targetUserId);
            case 3: // Complete
                return RedirectToAction("AdditionalDetails", targetUserId);
            case 4:
                return RedirectToAction("ReviewProfile", targetUserId);
            case 5:
                return RedirectToAction("Index");//dashboard index
            default:
                return RedirectToAction("RegisterClient");
        }
    }

    [HttpGet]
    [Route("Clients/RegisterClient")]
    public async Task<IActionResult> RegisterClient()
    {
        //if already logged in and registraion incomplete, redirect to current step
        bool isCurrentUserAdmin = User.IsInRole("Admin");
        if (User.Identity != null && User.Identity.IsAuthenticated && !User.IsInRole("Admin"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile != null && profile.RegistrationStep < 3)
            {
                return RedirectToRegistrationStep(profile.RegistrationStep, null);
            }
            return RedirectToAction("Index", "Home");
        }
        if (isCurrentUserAdmin)
        {
            ViewBag.targetUserId = "targetUserId";//pass to view for consiquent submission
        }
        return View();
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
                return BadRequest(new { success = false, type = "warning", message = "You must be at least 18 years old." });
            }
        }
        catch
        {
            return BadRequest(new { success = false, type = "warning", message = "Invalid date of birth." });
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
        // Ensure Admin role exists
        if (!await _roleManager.RoleExistsAsync("Client"))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole("Client"));
            if (!roleResult.Succeeded)
            {
                return BadRequest(new { success = false, message = string.Join("; ", roleResult.Errors.Select(e => e.Description)) });
            }
        }
        try
        {
            // Create user with password
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.FirstName + " " + model.LastName,
                CountryCode = model.PhoneCode,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = new DateTime(model.DobYear, model.DobMonth, model.DobDay).ToUniversalTime(),
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new { success = false, message = $"Failed to create user: {errors}" });
            }
            // Add to Client role (optional)
            await _userManager.AddToRoleAsync(user, "Client");

            // Log in the user immediately
            await _signInManager.SignInAsync(user, isPersistent: false);

            // Create a client profile with step 1 (basic complete, ready for employement & income)
            var userProfile = new ClientProfile
            {
                UserId = user.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CountryOfResidence = model.CountryOfResidence,
                AccountType = model.AccountType,
                MarketingConsent = model.MarketingConsent,
                CreatedOn = DateTime.UtcNow,
                RegistrationStep = 1
            };
            _context.ClientProfiles.Add(userProfile);
            var profileResult = await _context.SaveChangesAsync();

            if (profileResult == 0)
            {
                throw new Exception("Failed to create user profile.");
            }
            await transaction.CommitAsync();

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
            return StatusCode(500, new { success = false, message = "An error occurred while processing the request. Please try again." });
        }
    }

    // POST: Admin creates basic info (no password, sends reset email)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [Route("Clients/AdminSaveBasicInfo")]
    public async Task<IActionResult> AdminSaveBasicInfo([FromBody] BasicInfoModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid data submitted." });
        }

        // Validate DOB (same as above)
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

        // Check email uniqueness (same as above)
        if (string.IsNullOrEmpty(model.Email))
        {
            return BadRequest(new { success = false, message = "Email is required." });
        }
        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            return BadRequest(new { success = false, message = "Email is already registered." });
        }

        var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create user WITHOUT password
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.FirstName + " " + model.LastName,
                CountryCode = model.PhoneCode,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = new DateTime(model.DobYear, model.DobMonth, model.DobDay).ToUniversalTime(),
            };
            var result = await _userManager.CreateAsync(user); // No password
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new { success = false, message = $"Failed to create user: {errors}" });
            }

            // Add to Client role (optional)
            await _userManager.AddToRoleAsync(user, "Client");

            // Generate password reset token and send email
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            //await SendPasswordResetEmail(user.Email, token); // Implement this method to send email with reset link, e.g., Url.Action("ResetPassword", "Account", new { token, email = user.Email })

            // Create client profile with step 1
            var userProfile = new ClientProfile
            {
                UserId = user.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CountryOfResidence = model.CountryOfResidence,
                AccountType = model.AccountType,
                MarketingConsent = model.MarketingConsent,
                CreatedOn = DateTime.UtcNow,
                RegistrationStep = 1
            };
            _context.ClientProfiles.Add(userProfile);
            var profileResult = await _context.SaveChangesAsync();

            if (profileResult == 0)
            {
                throw new Exception("Failed to create user profile.");
            }
            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = "Client created. Password reset email sent. Proceed to next step if needed.",
                redirectUrl = Url.Action("IncomeInfo", "Clients", new { targetUserId = user.Id }) // Allow admin to continue
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating user or profile");
            return StatusCode(500, new { success = false, message = "An error occurred while processing your request. Please try again." });
        }
    }

    // Helper: Resolves userId and fetches user/profile with authorization checks
    private async Task<(bool isCurrentUserAdmin, ApplicationUser? User, ClientProfile? Profile, IActionResult? ErrorResult)> ResolveUserAndProfileAsync(string targetUserId, int expectedCompletedStep)
    {
        string userId;
        bool isCurrentUserAdmin = User.IsInRole("Admin");

        // Determine userId based on targetUserId (admin) or current user
        if (!string.IsNullOrEmpty(targetUserId))
        {
            if (!isCurrentUserAdmin)
            {
                return (isCurrentUserAdmin, null, null, Unauthorized());
            }
            userId = targetUserId;
        }
        else
        {
            var foundUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(foundUserId))
            {
                return (isCurrentUserAdmin, null, null, Unauthorized());
            }
            userId = foundUserId;
        }

        // Fetch user
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (isCurrentUserAdmin, null, null, NotFound());
        }

        // Fetch profile
        var profile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            return (isCurrentUserAdmin, null, null, RedirectToAction("RegisterClient"));
        }

        // Enforce registration step for non-admins
        if (!isCurrentUserAdmin && profile.RegistrationStep < expectedCompletedStep)
        {
            return (isCurrentUserAdmin, null, null, RedirectToRegistrationStep(profile.RegistrationStep, targetUserId));
        }

        return (isCurrentUserAdmin, user, profile, null);
    }

    [HttpGet]
    [Authorize]
    [Route("Clients/IncomeInfo")]
    public async Task<IActionResult> IncomeInfo(string? targetUserId = null)
    {
        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 1);
        if (errorResult != null)
        {
            return errorResult;
        }

        if (profile == null)
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
        ViewBag.targetUserId = targetUserId;//pass to view for consiquent submission
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [Route("Clients/SaveIncomeInfo")]
    public async Task<IActionResult> SaveIncomeInfo([FromBody] EmploymentInfoModel model, string? targetUserId = null)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid data submitted." });
        }

        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 1);
        if (errorResult != null)
        {
            return errorResult;
        }

        if (profile == null)
        {
            return RedirectToAction("RegisterClient");
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
            profile.RegistrationStep = 2;

            _context.ClientProfiles.Update(profile);
            var result = await _context.SaveChangesAsync();

            if (result == 0)
            {
                throw new Exception("Failed to create user profile.");
            }

            await saveIncomeTransaction.CommitAsync();
            string? redirectUrl = isAdmin
                                ? (user != null ? Url.Action("TradingInfo", "Clients", new { targetUserId = user.Id }) : null)
                                : Url.Action("TradingInfo", "Clients");
            return Ok(new
            {
                success = true,
                message = "Employment and income information saved.",
                redirectUrl
            });
        }
        catch (Exception ex)
        {
            await saveIncomeTransaction.RollbackAsync();
            _logger.LogError(ex, "Error saving income info for user {UserId}", user != null ? user.Id : "unknown");
            return StatusCode(500, new { success = false, message = "An error occurred while saving income information. Please try again." });
        }
    }

    [HttpGet]
    [Authorize]
    [Route("Clients/TradingInfo")]
    public async Task<IActionResult> TradingInfo(string? targetUserId = null)
    {
        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 2);
        if (errorResult != null)
        {
            return errorResult;
        }

        if (profile == null)
        {
            return RedirectToAction("RegisterClient");
        }

        var model = new TradingExperienceModel
        {
            YearsOfExperience = profile.YearsOfTradingExperience,
            ConfirmTradingKnowledge = profile.ConfirmTradingKnowledge,
        };
        ViewBag.targetUserId = targetUserId;//pass to view for consiquent submission
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [Route("Clients/SaveTradingInfo")]
    public async Task<IActionResult> SaveTradingInfo([FromBody] TradingExperienceModel model, string? targetUserId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid data submitted." });
        }

        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 2);
        if (errorResult != null)
        {
            return errorResult;
        }

        if (profile == null)
        {
            return RedirectToAction("RegisterClient");
        }

        var tradingInfoTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Update profile with trading experience info
            profile.YearsOfTradingExperience = model.YearsOfExperience;
            profile.ConfirmTradingKnowledge = model.ConfirmTradingKnowledge;
            profile.RegistrationStep = 3;
            _context.ClientProfiles.Update(profile);
            var result = await _context.SaveChangesAsync();

            if (result == 0)
            {
                throw new Exception("Failed to create user profile.");
            }

            await tradingInfoTransaction.CommitAsync();
            string? redirectUrl = isAdmin
                                            ? (user != null ? Url.Action("AdditionalDetails", "Clients", new { targetUserId = user.Id }) : null)
                                            : Url.Action("AdditionalDetails", "Clients");
            return Ok(new
            {
                success = true,
                message = "Trading experience information saved. Registration complete.",
                redirectUrl
            });
        }
        catch (Exception ex)
        {
            await tradingInfoTransaction.RollbackAsync();
            _logger.LogError(ex, "Error saving trading info for user {UserId}", user != null ? user.Id : "unknown");
            return StatusCode(500, new { success = false, message = "An error occurred while saving trading information. Please try again." });
        }
    }

    [HttpGet]
    [Authorize]
    [Route("Clients/AdditionalDetails")]
    public async Task<IActionResult> AdditionalDetails(string? targetUserId = null)
    {
        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 3);
        if (errorResult != null)
        {
            return errorResult;
        }

        if (profile == null)
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
        ViewBag.targetUserId = targetUserId;//pass to view for consiquent submission
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [Route("Clients/SaveAdditionalDetails")]
    public async Task<IActionResult> SaveAdditionalDetails([FromBody] AdditionalDetailsModel model, string? targetUserId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid data submitted." });
        }

        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 3);
        if (errorResult != null)
        {
            return errorResult;
        }

        if (profile == null)
        {
            return RedirectToAction("RegisterClient");
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
            profile.RegistrationStep = 4;
            _context.ClientProfiles.Update(profile);
            var result = await _context.SaveChangesAsync();
            if (result == 0)
            {
                throw new Exception("Failed to create user profile.");
            }
            await additionalDetailsTransaction.CommitAsync();

            string? redirectUrl = isAdmin
                                    ? (user != null ? Url.Action("ReviewProfile", "Clients", new { targetUserId = user.Id }) : null)
                                    : Url.Action("ReviewProfile", "Clients");
            return Ok(new
            {
                success = true,
                message = "Additional details saved. Registration complete.",
                redirectUrl
            });

        }
        catch (Exception ex)
        {
            await additionalDetailsTransaction.RollbackAsync();
            _logger.LogError(ex, "Error saving additional details for user {UserId}", user != null ? user.Id : "unknown");
            return StatusCode(500, new { success = false, message = "An error occurred while saving additional details. Please try again." });
        }
    }

    [HttpGet]
    [Authorize]
    [Route("Clients/ReviewProfile")]
    public async Task<IActionResult> ReviewProfile(string? targetUserId = null)
    {
        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 4);
        if (errorResult != null)
        {
            return errorResult;
        }

        if (profile == null || user == null)
        {
            return RedirectToAction("RegisterClient");
        }
        // var user = await _userManager.FindByIdAsync("67070c44-88c3-4a7e-8e1b-4fbc3e078b56");
        // var profile = _context.ClientProfiles.FirstOrDefault(p => p.UserId == "67070c44-88c3-4a7e-8e1b-4fbc3e078b56");
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
        ViewBag.targetUserId = targetUserId;//pass to view for consiquent submission
        return View(model);
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
    [Authorize]
    [ValidateAntiForgeryToken]
    [Route("Clients/UpdateProfileData")]
    public async Task<IActionResult> UpdateProfileData([FromBody] UpdateProfileDTO model, string? targetUserId = null)
    {
        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 4);
        if (errorResult != null)
        {
            return errorResult;
        }

        if (profile == null || user == null)
        {
            return RedirectToAction("RegisterClient");
        }

        string successMessage = string.Empty;
        List<string> missingFields = new List<string>();
        string? redirectUrl = null;
        var updateProfileDataTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (model != null)
            {
                // var clientProfile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == model.ProfileData.UserId);
                // ApplicationUser? user = null;
                // if (!string.IsNullOrEmpty(model.ProfileData.UserId))
                // {
                //     user = await _userManager.FindByIdAsync(model.ProfileData.UserId);
                // }
                // if (clientProfile == null || user == null)
                // {
                //     return StatusCode(500, new { success = false, message = "Profile not found." });
                // }
                switch (model.ProfileSection)
                {
                    case "PersonalDetails":
                        missingFields = ValidateRequiredFields(model.ProfileData,
                           nameof(model.ProfileData.UserId),
                           nameof(model.ProfileData.FirstName),
                           nameof(model.ProfileData.LastName),
                           nameof(model.ProfileData.Email),
                           nameof(model.ProfileData.PhoneCode),
                           nameof(model.ProfileData.PhoneNumber),
                           nameof(model.ProfileData.AccountType),
                           nameof(model.ProfileData.DateOfBirth),
                           nameof(model.ProfileData.CountryOfResidence)
                       );

                        if (missingFields.Any())
                        {
                            return BadRequest(new { success = false, message = "Missing required fields: " + string.Join(", ", missingFields) });
                        }

                        //update client profile fields
                        profile.FirstName = model.ProfileData.FirstName;
                        profile.LastName = model.ProfileData.LastName;
                        profile.AccountType = model.ProfileData.AccountType;
                        profile.CountryOfResidence = model.ProfileData.CountryOfResidence;

                        //update client user fields
                        user.Name = model.ProfileData.FirstName + " " + model.ProfileData.LastName;
                        user.CountryCode = model.ProfileData.PhoneCode;
                        user.PhoneNumber = model.ProfileData.PhoneNumber;
                        user.DateOfBirth = model.ProfileData.DateOfBirth?.ToUniversalTime();
                        //update email if changed
                        if (user.Email != model.ProfileData.Email)
                        {
                            user.UserName = model.ProfileData.Email;
                            user.Email = model.ProfileData.Email;
                            // Optionally: Send email confirmation
                            // var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            // await SendEmailConfirmation(user.Email, token);
                        }

                        var userResult = await _userManager.UpdateAsync(user);
                        if (!userResult.Succeeded)
                        {
                            return StatusCode(500, new { success = false, message = "Unable to update client user." });
                        }

                        successMessage = "Personal details updated successfully.";
                        break;

                    case "Employment&Income":
                        missingFields = ValidateRequiredFields(model.ProfileData,
                            nameof(model.ProfileData.EmploymentStatus),
                            nameof(model.ProfileData.AnnualIncome),
                            nameof(model.ProfileData.PrimarySourceOfTradingFund),
                            nameof(model.ProfileData.TradingObjective),
                            nameof(model.ProfileData.DegreeOfRisk)
                        );

                        if (missingFields.Any())
                        {
                            return BadRequest(new { success = false, message = "Missing required fields: " + string.Join(", ", missingFields) });
                        }
                        if (profile != null)
                        {
                            profile.EmploymentStatus = model.ProfileData.EmploymentStatus;
                            profile.AnnualIncome = model.ProfileData.AnnualIncome;
                            profile.PrimarySourceOfTradingFund = model.ProfileData.PrimarySourceOfTradingFund;
                            profile.TradingObjective = model.ProfileData.TradingObjective;
                            profile.DegreeOfRisk = model.ProfileData.DegreeOfRisk;
                        }
                        successMessage = "Employment and income details updated successfully.";
                        break;

                    case "TradingPreference":
                        missingFields = ValidateRequiredFields(model.ProfileData,
                            nameof(model.ProfileData.YearsOfTradingExperience),
                            nameof(model.ProfileData.ConfirmTradingKnowledge)
                        );

                        if (missingFields.Any())
                        {
                            return BadRequest(new { success = false, message = "Missing required fields: " + string.Join(", ", missingFields) });
                        }
                        if (profile != null)
                        {
                            profile.YearsOfTradingExperience = model.ProfileData.YearsOfTradingExperience;
                            profile.ConfirmTradingKnowledge = model.ProfileData.ConfirmTradingKnowledge;
                        }
                        successMessage = "Trading preferences updated successfully.";
                        break;

                    case "AdditionalDetails":
                        missingFields = ValidateRequiredFields(model.ProfileData,
                            nameof(model.ProfileData.Street),
                            nameof(model.ProfileData.City),
                            nameof(model.ProfileData.Nationality),
                            nameof(model.ProfileData.PlaceOfBirth)
                        );

                        if (missingFields.Any())
                        {
                            return BadRequest(new { success = false, message = "Missing required fields: " + string.Join(", ", missingFields) });
                        }
                        if (profile != null)
                        {
                            profile.BuildingNumber = model.ProfileData.BuildingNumber;
                            profile.Street = model.ProfileData.Street;
                            profile.PostalCode = model.ProfileData.PostalCode;
                            profile.Nationality = model.ProfileData.Nationality;
                            profile.PlaceOfBirth = model.ProfileData.PlaceOfBirth;
                        }
                        successMessage = "Additional details updated successfully.";
                        break;

                    case "Declaration":
                        missingFields = ValidateRequiredFields(model.ProfileData,
                            nameof(model.ProfileData.IsAcceptTermsandConditions)
                        );

                        if (missingFields.Any())
                        {
                            return BadRequest(new { success = false, message = "Missing required fields: " + string.Join(", ", missingFields) });
                        }
                        if (profile != null)
                        {
                            profile.IsAcceptTermsandConditions = model.ProfileData.IsAcceptTermsandConditions;
                            profile.RegistrationStep = 5;
                        }
                        successMessage = "Declartion updated successfully.";
                        redirectUrl = isAdmin
                                    ? (user != null ? Url.Action("VerifyProfile", "Clients", new { targetUserId = user.Id }) : null)
                                    : Url.Action("VerifyProfile", "Clients");
                        break;

                    case "Verification":
                        break;
                    default:
                        return BadRequest(new { success = false, message = "Invalid profile section." });
                }

                if (profile == null)
                {
                    await updateProfileDataTransaction.RollbackAsync();
                    return RedirectToAction("RegisterClient");
                }

                _context.ClientProfiles.Update(profile);

                var saveResult = await _context.SaveChangesAsync();
                if (saveResult == 0)
                {
                    await updateProfileDataTransaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "Failed to update profile." });
                }
                await updateProfileDataTransaction.CommitAsync();

                return Ok(new { success = true, message = successMessage, updatedRecord = profile, redirectUrl });

            }
            return BadRequest(new { success = false, message = "Invalid form data." });
        }
        catch (Exception ex)
        {
            await updateProfileDataTransaction.RollbackAsync();
            _logger.LogError(ex, "Error updating profile for user {UserId}, section {Section}", user != null ? user.Id : "unknown", model != null ? model.ProfileSection : "unknown");
            return BadRequest(new { success = false, message = "An error occurred while updating the profile." });
        }
    }

    [HttpGet]
    [Authorize]
    [Route("Clients/VerifyProfile")]
    public async Task<IActionResult> VerifyProfile(string? targetUserId = null)
    {
        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 5);
        if (errorResult != null)
        {
            return errorResult;
        }
        ViewBag.targetUserId = targetUserId;//pass to view for consiquent submission
        return View();
    }

    private enum Documents
    {
        Passport = 1,
        GovernmentID,
        License,
        BankStatement,
        UtilityBill
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [Route("Clients/DocumentUpload")]
    public async Task<IActionResult> DocumentUpload(string documentType, IFormFile file, string documentSection, string? targetUserId = null)
    {
        var (isAdmin, user, profile, errorResult) = await ResolveUserAndProfileAsync(targetUserId ?? string.Empty, expectedCompletedStep: 5);
        if (errorResult != null)
        {
            return errorResult;
        }
        if (string.IsNullOrEmpty(documentType))
        {
            return BadRequest(new { success = false, message = "Document type is required." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "No file uploaded." });
        }

        // Validate file size (5MB limit)
        // if (file.Length > 5 * 1024 * 1024)
        // {
        //     return BadRequest(new { success = false, message = "File size exceeds 5MB limit." });
        // }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(new { success = false, message = "Invalid file type. Only images and PDFs are allowed." });
        }

        // Validate document type based on section
        string[] validDocumentTypes = documentSection switch
        {
            "governmentDocument" => new[] { "license", "passport", "idcard" },
            "proofOfAddDocument" => new[] { "bankstatement", "utilitybill" },
            _ => Array.Empty<string>()
        };

        if (!validDocumentTypes.Contains(documentType.ToLower()))
        {
            return BadRequest(new { success = false, message = "Invalid document type." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {

            // Generate unique file name
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_uploadPath, uniqueFileName);

            // Save file
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Parse enum safely
            if (!Enum.TryParse<Documents>(documentType, true, out var documentEnum))
            {
                return BadRequest(new { success = false, message = "Invalid document type." });
            }

            // Save document info in DB
            var document = new UserDocument
            {
                UserId = targetUserId != null ? targetUserId : (user != null ? user.Id : string.Empty),
                DocumentId = uniqueFileName,
                DocumentName = file.FileName,
                DocumentType = (int)documentEnum
            };

            _context.UserDocuments.Add(document);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var redirectUrl = string.Empty;

            // documentSection == "proofOfAddDocument"
            //     ? Url.Action("Index", "Home")
            //     : string.Empty;

            if (documentSection == "proofOfAddDocument")
            {
                redirectUrl = isAdmin
                            ? (user != null ? Url.Action("Index", "Clients", new { targetUserId = user.Id }) : null)
                            : Url.Action("Index", "Clients");//client dashboard
            }

            return Ok(new
            {
                success = true,
                message = "Document uploaded successfully.",
                redirectUrl
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error while saving document for user {UserId}", user != null ? user.Id : "unknown");
            return StatusCode(500, new { success = false, message = "An error occurred while saving additional details. Please try again." });
        }
    }

}
