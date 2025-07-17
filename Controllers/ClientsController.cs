using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CRM.Models;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IActionResult> Index()
    {
        var users = await _context.users.ToListAsync();
        return View(users);
    }  

}
