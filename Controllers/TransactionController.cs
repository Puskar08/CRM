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
                           join acc in _context.ClientAccounts on tr.Mt5LoginID equals acc.Mt5LoginID
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
        if (!string.IsNullOrEmpty(filterModel.FilterStartDate) && DateTime.TryParse(filterModel.FilterStartDate, out DateTime startDate))
        {
            startDateUtc = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        }
        if (!string.IsNullOrEmpty(filterModel.FilterEndDate) && DateTime.TryParse(filterModel.FilterEndDate, out DateTime endDate))
        {
            endDateUtc = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        }
        //base query
        var query = from tr in _context.Transactions
                    join acc in _context.ClientAccounts on tr.Mt5LoginID equals acc.Mt5LoginID
                    join user in _context.Users on acc.UserId equals user.Id
                    //    where
                    //        ((string.IsNullOrEmpty(filterModel.FilterTransactionType) || filterModel.FilterTransactionType == "All") ? 1 == 1 : tr.TransactionType == filterModel.FilterTransactionType)
                    //        && ((string.IsNullOrEmpty(filterModel.FilterStatus) || filterModel.FilterStatus == "All") ? 1 == 1 : tr.Status == (filterModel.FilterStatus == "Approved" ? 1 : filterModel.FilterStatus == "Rejected" ? 2 : 0))
                    //        && (!startDateUtc.HasValue ? 1 == 1 : tr.TransactionDate >= startDateUtc.Value)
                    //         && (!endDateUtc.HasValue ? 1 == 1 : tr.TransactionDate <= endDateUtc.Value)
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
                    };
        if (!string.IsNullOrEmpty(filterModel.GlobalSearch))
        {
            var search = filterModel.GlobalSearch.ToLower();
            query = query.Where(t =>
                t.TransactionId.ToString().Contains(search) ||
                t.Login.ToString().ToLower().Contains(search) ||
                (t.UserName != null && t.UserName.ToLower().Contains(search)) ||
                (t.TransactionType != null && t.TransactionType.ToLower().Contains(search)) ||
                t.Amount.ToString().Contains(search) ||
                t.Fee.ToString().Contains(search) ||
                t.ApprovalStatus.ToString().Contains(search) ||
                t.TransactionDate.ToString().Contains(search));
        }

        // Apply filters
        //filter transactionId
        if (!string.IsNullOrEmpty(filterModel.FilterTransactionId))
        {
            if (int.TryParse(filterModel.FilterTransactionId, out int transId))
            {
                query = query.Where(t => t.TransactionId == transId);
            }
        }
        //filter login
        if (!string.IsNullOrEmpty(filterModel.FilterLogin))
        {
            if (int.TryParse(filterModel.FilterLogin, out int loginId))
            {
                query = query.Where(t => t.Login == loginId);
            }
        }
        //filter client username
        if (!string.IsNullOrEmpty(filterModel.FilterClient))
        {
            query = query.Where(t => t.UserName != null && t.UserName.Contains(filterModel.FilterClient));
        }
        //filter transaction type
        if (!string.IsNullOrEmpty(filterModel.FilterTransactionType) && filterModel.FilterTransactionType != "All")
        {
            query = query.Where(t => t.TransactionType == filterModel.FilterTransactionType);
        }
        //filter status
        if (!string.IsNullOrEmpty(filterModel.FilterStatus) && filterModel.FilterStatus != "All" && filterModel.FilterStatus != "-1")
        {
            if (int.TryParse(filterModel.FilterStatus, out int status))
            {
                query = query.Where(t => t.ApprovalStatus == status);
            }
        }
        //filter date range
        if (startDateUtc.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDateUtc.Value);
        }
        if (endDateUtc.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDateUtc.Value);
        }
        //filter amount
        if (!string.IsNullOrEmpty(filterModel.FilterAmount) && filterModel.FilterAmount != "All")
        {
            if (int.TryParse(filterModel.FilterAmount, out int amount))
            {
                switch (amount)
                {
                    case 1:
                        query = query.Where(t => t.Amount >= 0 && t.Amount <= 100);
                        break;
                    case 2:
                        query = query.Where(t => t.Amount > 100 && t.Amount <= 500);
                        break;
                    case 3:
                        query = query.Where(t => t.Amount >= 500 && t.Amount <= 1000);
                        break;
                    case 4:
                        query = query.Where(t => t.Amount >= 1000);
                        break;
                }
            }
        }
        //filter fee
        if (!string.IsNullOrEmpty(filterModel.FilterFee))
        {
            if (decimal.TryParse(filterModel.FilterFee, out decimal fee))
            {
                query = query.Where(t => t.Fee == fee);
            }
        }
        // Get total records count before pagination
        int totalRecords = await _context.Transactions.CountAsync();
        // Get filtered records count before pagination
        int filteredRecords = await query.CountAsync();
        // Apply sorting
        if (!string.IsNullOrEmpty(filterModel.SortColumn) && !string.IsNullOrEmpty(filterModel.SortDirection))
        {
            bool ascending = filterModel.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
            query = filterModel.SortColumn switch
            {
                "TransactionId" => ascending ? query.OrderBy(t => t.TransactionId) : query.OrderByDescending(t => t.TransactionId),
                "Login" => ascending ? query.OrderBy(t => t.Login) : query.OrderByDescending(t => t.Login),
                "UserName" => ascending ? query.OrderBy(t => t.UserName) : query.OrderByDescending(t => t.UserName),
                "TransactionType" => ascending ? query.OrderBy(t => t.TransactionType) : query.OrderByDescending(t => t.TransactionType),
                "Amount" => ascending ? query.OrderBy(t => t.Amount) : query.OrderByDescending(t => t.Amount),
                "Fee" => ascending ? query.OrderBy(t => t.Fee) : query.OrderByDescending(t => t.Fee),
                "Status" => ascending ? query.OrderBy(t => t.Status) : query.OrderByDescending(t => t.Status),
                "TransactionDate" => ascending ? query.OrderBy(t => t.TransactionDate) : query.OrderByDescending(t => t.TransactionDate),
                _ => query.OrderByDescending(t => t.TransactionDate), // Default sorting
            };
        }
        else
        {
            // Default sorting
            query = query.OrderByDescending(t => t.TransactionDate);
        }
        // Apply pagination
        int page = filterModel.Page <= 0 ? 1 : filterModel.Page;
        int pageSize = filterModel.PageSize <= 0 ? 10 : filterModel.PageSize;
        query = query.Skip((page - 1) * pageSize).Take(pageSize);
        var transactions = await query.ToListAsync();
        var response = new FilterResponseModel
        {
            Data = transactions,
            TotalRecords = totalRecords,
            FilteredRecords = filteredRecords,
            PageSize = pageSize,
            CurrentPage = page
        };
        return Ok(response);
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
                           join account in _context.ClientAccounts
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
                      join acc in _context.ClientAccounts on tr.Mt5LoginID equals acc.Mt5LoginID into accJoin
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