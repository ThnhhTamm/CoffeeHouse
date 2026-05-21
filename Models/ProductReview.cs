namespace CoffeeHouseAdmin.Models
{
    public class ProductReview
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string CustomerName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}