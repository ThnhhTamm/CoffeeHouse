using Microsoft.AspNetCore.Mvc;
using CoffeeHouseAdmin.Models;
using Microsoft.EntityFrameworkCore;
using CoffeeHouseAdmin.Data;

namespace CoffeeHouseAdmin.Controllers
{
    // 1. Class hứng dữ liệu (Phải khớp 100% với tên biến trong JavaScript)
   public class BookingRequest
{
    public string TableID { get; set; } // PHẢI LÀ STRING Boss nhé!
    public string Phone { get; set; }
    public string Date { get; set; }
    public string Time { get; set; }
    public int PartySize { get; set; }
}
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tables = await _context.CoffeeTables.ToListAsync();
            return View(tables);
        }

        [HttpPost]
      [HttpPost]
public async Task<IActionResult> ConfirmBooking([FromBody] BookingRequest req)
{
    if (req == null) return Json(new { success = false, message = "Dữ liệu trống!" });

    try 
    {
        var userEmail = User.Identity?.Name ?? "Guest";

        // ÉP KIỂU Ở ĐÂY ĐỂ HẾT LỖI CS0029
        var newBooking = new TableBooking
        {
            CustomerEmail = userEmail,
            CustomerPhone = req.Phone,
            CoffeeTableID = req.TableID, // Đổi string sang int
            BookingDate = DateTime.Parse(req.Date),
            BookingTime = req.Time,
            NumberOfPeople = req.PartySize,
            Status = "Đã đặt"
        };

        _context.TableBookings.Add(newBooking);
        
        // TÌM BÀN CŨNG PHẢI ÉP KIỂU
        var tableIdInt = int.Parse(req.TableID);
        var table = await _context.CoffeeTables.FindAsync(tableIdInt);
        
        if (table != null) 
        {
            table.Status = "Hết bàn";
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Lỗi: " + ex.Message });
    }
}
}
}