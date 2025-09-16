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

    public IActionResult Index()
    {
        //var users = await _context.users.ToListAsync();
        return View();
    }

    public Task<IActionResult> Register()
    {
        return Task.FromResult<IActionResult>(View());
    }
    public Task<IActionResult> RegisterClient()
    {
        return Task.FromResult<IActionResult>(View());
    }
    // POST: Client/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterClientViewModel client, List<IFormFile> documents)
    {
        if (ModelState.IsValid)
        {
            // Handle file uploads
            if (documents != null && documents.Count > 0)
            {
                var paths = new List<string>();
                foreach (var file in documents)
                {
                    if (file.Length > 0)
                    {
                        // Save the file to a designated folder
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        
                        paths.Add(fileName);
                    }
                }
                client.DocumentPaths = string.Join(",", paths);
            }
            
            client.CreatedDate = DateTime.Now;
            _context.Add(client);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Success));
        }
        
        return View(client);
    }
    
    public IActionResult Success()
    {
        return View();
    }

}
