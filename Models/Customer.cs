namespace CoffeeHouseAdmin.Models
{
    public class Customer
    {
        public string CustomerID { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public string Rank { get; set; } // Hạng: Đồng, Bạc, Vàng, Kim Cương...
    }
}