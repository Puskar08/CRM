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
        var trans = (from tr in _context.Transactions
                     join acc in _context.ClientsAccounts on tr.Mt5LoginID equals acc.Mt5LoginID
                     join user in _context.Users on acc.UserId equals user.Id
                     select new TransactionViewModel
                     {
                         TransactionId = tr.TransactionId,
                         Login = acc.Mt5LoginID,
                         UserName = user.Name,
                         TransactionType = tr.TransactionType,
                         Amount = tr.Amount,
                         Fee = tr.Fee,
                         Status = tr.Status == 1 ? "Approved" : tr.Status == 2 ? "Rejected" : "Pending",
                         ApprovalStatus = tr.Status,
                         TransactionDate = tr.TransactionDate
                     }).ToList();
        return View(trans);
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
                Status = form["actionType"] == "submitAndApprove" ? 1 : 0,
                TransactionDate = DateTime.UtcNow
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

    [HttpPost]
    public IActionResult UpdateTransactionStatus([FromBody] TransactionStatusUpdateModel data)
    {

        if (data.TransactionId <= 0 || data.ApprovalStatus < 0 || data.ApprovalStatus > 2) // Assuming 0: Pending, 1: Approved, 2: Rejected
        {
            return BadRequest(new { message = "Invalid input data." });
        }
        var transaction = _context.Transactions.FirstOrDefault(t => t.TransactionId == data.TransactionId && t.Status == 0); // Only pending transactions
        if (transaction == null)
        {
            return NotFound(new { message = "Transaction not found." });
        }

        if (data.ApprovalStatus == 1 || data.ApprovalStatus == 2)
        {
            transaction.Status = data.ApprovalStatus; // Update status based on input (1: Approved, 2: Rejected)
        }
        else
        {
            return BadRequest(new { message = "Invalid action type." });
        }
        _context.SaveChanges();
        return Ok(new { message = "Transaction status updated successfully." });
    }
}