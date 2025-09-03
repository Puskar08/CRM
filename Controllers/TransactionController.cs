using CRM.Models;
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

        var users = (from user in _context.Users
                     join account in _context.ClientsAccounts
                     on user.Id equals account.UserId
                     where (user.Name != null && user.Name.Contains(query))
                        || (user.Email != null && user.Email.Contains(query))
                     select new
                     {
                         user.Name,
                         user.Email,
                         account.Mt5LoginID
                     })
                        .Take(10)
                        .ToList();

        return Json(users);
    }

    [HttpPost]
    public IActionResult AddTransaction(IFormCollection form)
    {
        try
        {
            var transaction = new Transaction
            {
                Mt5LoginID = int.Parse(form["loginId"]),
                TransactionType = form["transactionType"],
                Amount = decimal.Parse(form["amount"]),
                Fee = decimal.Parse(form["fee"]),
                Description = form["transactionNote"],
                Status = form["actionType"] == "submitAndApprove" ? 1 : 0
            };
            transaction.UserId = "superAdmin";
            _context.Transactions.Add(transaction);
            _context.SaveChanges();
            return Ok(new { message = "Transaction added successfully." });
        }
        catch (Exception ex)
        {
            // Handle exception (e.g., log the error, return an error response)
            return BadRequest(new { message = "An error occurred while processing the transaction.", error = ex.Message });
        }


    }
    //add approval type in transaction
}