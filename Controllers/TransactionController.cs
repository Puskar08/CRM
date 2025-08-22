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
}
