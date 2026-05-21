using Microsoft.AspNetCore.Mvc;
using CoffeeHouseAdmin.Models;
using Microsoft.EntityFrameworkCore; // Quan trọng để dùng .Include()
using CoffeeHouseAdmin.Data; // Nhớ đổi đúng tên Project của Boss nhé

namespace CoffeeHouseAdmin.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TablesController : Controller
    {
        // 1. Gọi cái "Kho dữ liệu" (DbContext) ra để làm việc
        private readonly ApplicationDbContext _context;

        public TablesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 2. TRANG DANH SÁCH BÀN (Lấy từ SQL)
        public async Task<IActionResult> Index()
        {
            // Lấy toàn bộ bàn từ SQL Server lên
            var tables = await _context.CoffeeTables.ToListAsync();
            
            // Tính toán thống kê dựa trên dữ liệu THẬT
            ViewBag.Total = tables.Count;
            ViewBag.Available = tables.Count(t => t.Status == "Trống");
            ViewBag.Occupied = tables.Count(t => t.Status == "Đang phục vụ");
            ViewBag.Reserved = tables.Count(t => t.Status == "Đã đặt");

            return View(tables);
        }

        // 3. CẬP NHẬT TRẠNG THÁI (Lưu thẳng xuống SQL)
        [HttpPost]
public async Task<IActionResult> UpdateStatus(string id, string newStatus) // Sửa int thành string
        {var table = await _context.CoffeeTables.FindAsync(id); 
if (table != null) {
    table.Status = newStatus;
    await _context.SaveChangesAsync();
}
            return RedirectToAction("Index");
        }

        // 4. THÊM BÀN MỚI
        [HttpPost]
        public async Task<IActionResult> Create(CoffeeTable newTable)
        {
            if (ModelState.IsValid)
            {
                newTable.Status = "Trống"; 
                _context.CoffeeTables.Add(newTable);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}