using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    [Authorize]
    [Route("Home/CompleteProfile")]
    public async Task<IActionResult> CompleteProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if(user == null)
        {
            return RedirectToAction("Login", "Account");
        }
        var profile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if(profile == null)
        {
            return RedirectToAction("RegisterClient", "Clients");
        }
        var model = new ClientRegistrationModel
        {

            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            MarketingConsent = profile.MarketingConsent,
            EmploymentStatus = profile.EmploymentStatus,
            AnnualIncome = profile.AnnualIncome,
            PrimarySourceOfTradingFund = profile.PrimarySourceOfTradingFund,
            TradingObjective = profile.TradingObjective,
            DegreeOfRisk = profile.DegreeOfRisk,
            ConfirmTradingKnowledge = profile.ConfirmTradingKnowledge,
            BuildingNumber = profile.BuildingNumber,
            Street = profile.Street,
            City = profile.City,
            PostalCode = profile.PostalCode,
            Nationality = profile.Nationality,
            PlaceOfBirth = profile.PlaceOfBirth
        };
        return View(model);
    }
}
