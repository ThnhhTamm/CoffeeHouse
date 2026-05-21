using System;
namespace CoffeeHouseAdmin.Models{
public class Order
{
    public string OrderID { get; set; }
    public string CustomerName { get; set; }
    public string Phone { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public DateTime OrderDate { get; set; }
    // Để dấu ? để khách mua mang về (không ngồi bàn) vẫn đặt được nhé Boss!
    public string? TableID { get; set; }

    // Boss thêm dòng này vào để Dashboard hết báo lỗi nhé!
    public DateTime CreatedAt { get; set; } = DateTime.Now; 

    public string? PaymentMethod { get; set; }
        public string? ShippingAddress { get; set; }
}
}