using Microsoft.AspNetCore.Mvc;
using CRM.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace CRM.Controllers;

public class ClientsController : Controller
{
    private readonly ILogger<ClientsController> _logger;
    private readonly AppDbContext _context;
    private const string RegisterSessionKey = "RegistrationKey";
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

    // Helper: Get client registration from session
    private ClientRegistrationModel? GetClientRegistrationFromSession()
    {
        var json = HttpContext.Session.GetString(RegisterSessionKey);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<ClientRegistrationModel>(json);
    }

    // Helper: Save client registration to session
    private void SaveClientRegistrationToSession(ClientRegistrationModel registration)
    {
        var json = JsonSerializer.Serialize(registration);
        HttpContext.Session.SetString(RegisterSessionKey, json);
    }
    public Task<IActionResult> Register()
    {
        return Task.FromResult<IActionResult>(View());
    }
    public Task<IActionResult> RegisterClient()
    {
        //clear any existing session for new registration
        HttpContext.Session.Remove(RegisterSessionKey);
        return Task.FromResult<IActionResult>(View());
    }

    // Step 1: Basic Information
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBasicInfo([FromBody] BasicInfoModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("RegisterClient", model);
        }
        // Combine DOB and Phone
        var dateofBirth = new DateTime(model.DobYear, model.DobMonth, model.DobDay);
        var phone = $"{model.PhoneCode}{model.PhoneNumber}";
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
        // Create partial user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = $"{model.PhoneCode}{model.PhoneNumber}",
                // Country = model.Country,
                // AccountType = model.AccountType,
                // FirstName = model.FirstName,
                // LastName = model.LastName,
                 DateOfBirth = new DateTime(model.DobYear, model.DobMonth, model.DobDay),
                // MarketingConsent = model.MarketingConsent,
                // Status = "Pending"
            };
        // Load or create registration
        var registration = GetClientRegistrationFromSession() ?? new ClientRegistrationModel();
        registration.CountryOfResidence = model.CountryOfResidence;
        registration.AccountType = model.AccountType;
        registration.FirstName = model.FirstName;
        registration.LastName = model.LastName;
        registration.DateOfBirth = dateofBirth;
        registration.PhoneNumber = phone;
        registration.Email = model.Email;
        registration.Password = model.Password; // Temp store; will hash later
        registration.MarketingConsent = model.MarketingConsent;

        // SaveClientRegistrationToSession(registration);

        // Optional: Send email/SMS verification here (e.g., using SendGrid or Twilio)
        return Ok(new
        {
            success = true,
            message = "Basic information saved. Proceed to Employment Info.",
            redirectUrl = Url.Action("EmploymentInfo", "Clients")
        });
    }

    public Task<IActionResult> EmploymentInfo()
    {
        //clear any existing session for new registration
        // HttpContext.Session.Remove(RegisterSessionKey);
        return Task.FromResult<IActionResult>(View());
    }

}
