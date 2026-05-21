using Microsoft.AspNetCore.Mvc;
using CoffeeHouseAdmin.Models;
using CoffeeHouseAdmin.Data; // Đảm bảo có dòng này để gọi ApplicationDbContext
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoffeeHouseAdmin.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        // 1. Dùng Database thật thay vì List ảo
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 2. TRANG DANH SÁCH & TÌM KIẾM
        public async Task<IActionResult> Index(string search)
        {
            // Lấy dữ liệu từ Database
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(search) 
                                     || p.Category.ToLower().Contains(search));
            }

            ViewBag.SearchTerm = search;
            
            // Trả về danh sách thực tế từ DB
            var list = await query.OrderByDescending(p => p.Id).ToListAsync();
            ViewBag.Total = list.Count;
            
            return View(list);
        }

        // 3. CHỨC NĂNG THÊM MỚI (POST)
        [HttpPost]
        public async Task<IActionResult> Create(Product p)
        {
            if (ModelState.IsValid)
            {
                // Nếu không có ảnh thì lấy ảnh mặc định
                if (string.IsNullOrEmpty(p.ImageUrl)) 
                    p.ImageUrl = "https://images.unsplash.com/photo-1559525839-b184a4d698c7?w=100&q=80";

                _context.Add(p); // Database tự tăng ID, Boss không cần cộng 1 nữa nhé!
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. CHỨC NĂNG SỬA (POST)
        [HttpPost]
        public async Task<IActionResult> Edit(Product p)
        {
            var existing = await _context.Products.FindAsync(p.Id);
            if (existing != null)
            {
                existing.Name = p.Name;
                existing.Category = p.Category;
                existing.Price = p.Price;
                existing.Stock = p.Stock;
                existing.RoastLevel = p.RoastLevel;
                
                // BitternessLevel bây giờ là string nên gán trực tiếp thoải mái!
                existing.BitternessLevel = p.BitternessLevel; 
                existing.FlavorProfile = p.FlavorProfile;
                
                if (!string.IsNullOrEmpty(p.ImageUrl)) 
                    existing.ImageUrl = p.ImageUrl;

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 5. CHỨC NĂNG XÓA
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing != null)
            {
                _context.Products.Remove(existing);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}