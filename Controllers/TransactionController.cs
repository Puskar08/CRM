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
        // You can now use these variables to create a new transaction
        // For example:
        var transaction = new Transaction
        {
            Mt5LoginID = int.Parse(form["mt5LoginId"]),
            TransactionType = form["transactionType"],
            Amount = decimal.Parse(form["amount"]),
            Fee = decimal.Parse(form["fee"]),
            Description = form["transactionNote"],
            ApprovalStatus = form["submit"] == "submitAndApprove" ? 1 : 0
        };
        var actionType = form["actionType"];
        if (actionType == "submit")
        {

        }

        _context.Transactions.Add(transaction);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }
    //add approval type in transaction
}