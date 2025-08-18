using Microsoft.AspNetCore.Mvc;

namespace CRM.Controllers;

public class ClientsController : Controller
{
    private readonly ILogger<ClientsController> _logger;
    private readonly AppDbContext _context;

    public ClientsController(ILogger<ClientsController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public  IActionResult Index()
    {
        //var users = await _context.users.ToListAsync();
        return View();
    }  

}
