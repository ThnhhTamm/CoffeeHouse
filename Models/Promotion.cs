namespace CoffeeHouseAdmin.Models
{
    public class Promotion
    {
        public int PromoId { get; set; }
        public string PromoCode { get; set; }
        public int DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal MinOrderAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
    }
}