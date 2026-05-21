namespace CoffeeHouseAdmin.Models// Nhớ đổi YourProjectName thành tên Project của Boss
{
    public class CustomerAddress
    {
        public int AddressId { get; set; }
        public int CustomerId { get; set; }
        public string ReceiverName { get; set; }
        public string PhoneNumber { get; set; }
        public string AddressDetail { get; set; }
        public bool IsDefault { get; set; }
    }
}