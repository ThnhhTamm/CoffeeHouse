namespace CoffeeHouseAdmin.Models
{
    public class CustomerViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        // Hàm lấy 2 chữ cái đầu làm Avatar
        public string GetInitials() => Name.Length >= 2 ? Name.Substring(0, 2).ToUpper() : Name.ToUpper();
    }
}