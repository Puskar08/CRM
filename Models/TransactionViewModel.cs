namespace CRM.Models;

public class TransactionViewModel
{
    public int TransactionId { get; set; }
    public int Login { get; set; }
    public string? UserName { get; set; }
    public string? TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public string? Status { get; set; }
    public int ApprovalStatus { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class TransactionStatusUpdateModel
{
    public int TransactionId { get; set; }
    public int ApprovalStatus { get; set; }
}

public class TransactionFilterModel
{
    public string? FilterTransactionType { get; set; }
    public string? FilterStatus { get; set; }
    public string? FilterStartDate { get; set; }
    public string? FilterEndDate { get; set; }
    public string? FilterTransactionId { get; set; }
    public string? FilterLogin { get; set; }
    public string? FilterClient { get; set; }
    public string? FilterAmount { get; set; }
    public string? FilterFee { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15; // Pagination
    public string? SortColumn { get; set; } = "TransactionDate"; // Sorting
    public string? SortDirection { get; set; } = "desc"; // Sorting
}

public class FilterResponseModel
{
    public List<TransactionViewModel> Data { get; set; } = new List<TransactionViewModel>();
    public int TotalRecords { get; set; }
    public int FilteredRecords { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }

}