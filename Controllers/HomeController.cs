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

    
}
