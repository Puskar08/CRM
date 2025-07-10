using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Models;

[Table("users")]
public class Users
{
    public int id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
}