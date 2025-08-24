using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

public class TransactionController : Controller
{
    private readonly AppDbContext _context;

    public TransactionController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Deposit()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GetSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Json(new List<object>());
        }

        var users = _context.Users // Replace with your actual DbContext and User table
            .Where(u => (u.Name != null && u.Name.Contains(query)) || (u.Email != null && u.Email.Contains(query)))
            .Select(u => new
            {
                u.Name,
                u.Email
            })
            .Take(10) // Limit the number of suggestions
            .ToList();

        return Json(users);
    }
}
