namespace CRM.Models;
public class TransactionViewModel
{
    public int TransactionId { get; set; }
    public int Login { get; set; }
    public string UserName { get; set; }
    public string TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public string Status { get; set; }
    public int ApprovalStatus { get; set; }
    public DateTime TransactionDate { get; set; }
}
