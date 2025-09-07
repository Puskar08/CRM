using System.Security.Claims;
using CRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers;

public class TransactionController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICompositeViewEngine _viewEngine;
    public TransactionController(AppDbContext context, ICompositeViewEngine viewEngine)
    {
        _context = context;
        _viewEngine = viewEngine;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var trans = await (from tr in _context.Transactions
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
                           }).OrderByDescending(tr => tr.TransactionId).ToListAsync();
        return View(trans);
    }

    // AJAX endpoint to update table content
    [HttpPost]
    public async Task<IActionResult> GetFilteredTransactions([FromBody] TransactionFilterModel filterModel)
    {
        DateTime? startDateUtc = null;
        DateTime? endDateUtc = null;

        // Parse and convert to UTC, handling null or empty strings
        if (!string.IsNullOrEmpty(filterModel.StartDate) && DateTime.TryParse(filterModel.StartDate, out DateTime startDate))
        {
            startDateUtc = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        }
        if (!string.IsNullOrEmpty(filterModel.EndDate) && DateTime.TryParse(filterModel.EndDate, out DateTime endDate))
        {
            endDateUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        }
        var trans = await (from tr in _context.Transactions
                           join acc in _context.ClientsAccounts on tr.Mt5LoginID equals acc.Mt5LoginID
                           join user in _context.Users on acc.UserId equals user.Id
                           where
                               ((string.IsNullOrEmpty(filterModel.TransactionType) || filterModel.TransactionType == "All") ? 1 == 1 : tr.TransactionType == filterModel.TransactionType)
                               && ((string.IsNullOrEmpty(filterModel.Status) || filterModel.Status == "All") ? 1 == 1 : tr.Status == (filterModel.Status == "Approved" ? 1 : filterModel.Status == "Rejected" ? 2 : 0))
                               && (!startDateUtc.HasValue ? 1 == 1 : tr.TransactionDate >= startDateUtc.Value)
                                && (!endDateUtc.HasValue ? 1 == 1 : tr.TransactionDate <= endDateUtc.Value)
                           //    && (string.IsNullOrEmpty(filterModel.Login.ToString()) ? 1 == 1 : acc.Mt5LoginID.ToString() == filterModel.Login.ToString())
                           orderby tr.TransactionDate descending
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
                           }).ToListAsync();

        var html = "<tr><td colspan='9'><div class='no-data'><i class='fas fa-inbox'></i><p>No transactions found</p></div></td></tr>";
        if (trans.Count > 0)
        {
            html = await RenderViewAsync("_TransactionTable", trans);
        }
        return Ok(new { success = "true", html = html });
    }
    public async Task<string> RenderViewAsync<TModel>(string viewName, TModel model)
    {
        ViewData.Model = model;
        using (var writer = new StringWriter())
        {
            var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);
            if (viewResult.View == null)
            {
                //throw new ArgumentNullException($"{viewName} does not match any available view");
                // Return an empty table or default HTML instead of throwing an exception
                return "<tr><td colspan='9'><div class='no-data'><i class='fa fa-inbox'></i><p>No transactions found</p></div></td></tr>";
            }
            var viewContext = new ViewContext(
                ControllerContext,
                viewResult.View,
                ViewData,
                TempData,
                writer,
                new HtmlHelperOptions()
            );
            await viewResult.View.RenderAsync(viewContext);
            return writer.GetStringBuilder().ToString();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Json(new List<object>());
        }

        var users = await (from user in _context.Users
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
                        .ToListAsync();

        return Json(users);
    }

    public async Task<object?> GetTransaction(int transactionId)
    {
        return await (from tr in _context.Transactions
                      join acc in _context.ClientsAccounts on tr.Mt5LoginID equals acc.Mt5LoginID into accJoin
                      from acc in accJoin.DefaultIfEmpty()
                      join user in _context.Users on acc.UserId equals user.Id into userJoin
                      from user in userJoin.DefaultIfEmpty()
                      where tr.TransactionId == transactionId
                      select new
                      {
                          TransactionId = tr.TransactionId,
                          Login = acc != null ? acc.Mt5LoginID : tr.Mt5LoginID,
                          UserName = user != null ? user.Name : "Unknown",
                          TransactionType = tr.TransactionType,
                          Amount = tr.Amount,
                          Fee = tr.Fee,
                          Status = tr.Status == 1 ? "Approved" : tr.Status == 2 ? "Rejected" : "Pending",
                          ApprovalStatus = tr.Status,
                          TransactionDate = tr.TransactionDate.ToString("o") // ISO 8601
                      }).FirstOrDefaultAsync();

    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromForm] IFormCollection form)
    {
        try
        {
            // Validate form data
            if (!form.ContainsKey("loginId") || !form.ContainsKey("transactionType") ||
                !form.ContainsKey("amount") || !form.ContainsKey("fee"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Missing required form fields."
                });
            }

            // Parse form data with error handling
            if (!int.TryParse(form["loginId"], out int mt5LoginId) ||
                !decimal.TryParse(form["amount"], out decimal amount) ||
                !decimal.TryParse(form["fee"], out decimal fee))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid number format in loginId, amount, or fee."
                });
            }

            // Create transaction
            var transaction = new Transaction
            {
                Mt5LoginID = mt5LoginId,
                TransactionType = form["transactionType"].ToString() ?? string.Empty,
                Amount = amount,
                Fee = fee,
                Description = form["transactionNote"].ToString() ?? string.Empty,
                Status = form["actionType"] == "SubmitAndApprove" ? 1 : 0,
                TransactionDate = DateTime.UtcNow,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "superAdmin" // Get from auth or fallback
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var tran = await GetTransaction(transaction.TransactionId);
            if (tran == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction created but could not retrieve details."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Transaction added successfully.",
                transaction = tran
            });
        }
        catch (FormatException)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid data format in form submission."
            });
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Database error occurred while saving transaction."
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "An unexpected error occurred."
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTransactionStatus([FromBody] TransactionStatusUpdateModel data)
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
        await _context.SaveChangesAsync();
        return Ok(new { message = "Transaction status updated successfully.", success = true, transaction = transaction });
    }
}