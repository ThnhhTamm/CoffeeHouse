using System.ComponentModel.DataAnnotations;
namespace CoffeeHouseAdmin.Models
{
    public class CoffeeTable
{
    // 1. Sửa kiểu dữ liệu từ 'int' thành 'string' Boss nhé!
    [Key] // Nếu Boss dùng DataAnnotations
    public string TableID { get; set; } 

    public string TableName { get; set; }
    
    public int Capacity { get; set; }
    
    public string Status { get; set; }
    
    public string Location { get; set; }
}
}