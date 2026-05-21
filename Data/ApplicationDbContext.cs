using Microsoft.EntityFrameworkCore;
using CoffeeHouseAdmin.Models; // Chỗ này Boss check xem namespace của Product là gì nhé

namespace CoffeeHouseAdmin.Data 
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Đây là cái bàn đạp để lấy dữ liệu Product từ DB ra nè Boss
        public DbSet<Product> Products { get; set; } 
        public DbSet<TableBooking> TableBookings { get; set; }
       
        public DbSet<CoffeeTable> CoffeeTables { get; set; } // <--- Thêm dòng này nè Boss!
        public DbSet<Order> Orders { get; set; } // Thêm dòng này để Admin hết báo lỗi!
          // public DbSet<OrderItem> OrderItems { get; set; }

    }
}