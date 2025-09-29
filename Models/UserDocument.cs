using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.SignalR;

namespace CRM.Models;

public class UserDocument
{
    [Key]
    public int Id{ get; set; }
    public string? UserId { get; set; }
    public int DocumentType { get; set; }
    public string? DocumentId { get; set; }
    public string? DocumentName { get; set; }

}