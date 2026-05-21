using Microsoft.AspNetCore.Mvc;
using CoffeeHouseAdmin.Models;
using CoffeeHouseAdmin.Data; 
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Npgsql; // 🌟 Bộ dịch PostgreSQL chuẩn quốc tế
using System.Collections.Generic;
using System;

namespace CoffeeHouseAdmin.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hàm tiện ích để lấy chuỗi kết nối an toàn tuyệt đối, dán cứng mật khẩu bảo mật Render
       // Hàm tiện ích lấy chuỗi kết nối NỘI BỘ SIÊU TỐC - Không sợ bị ngắt SSL ngang xương
// Hàm lấy chuỗi kết nối NỘI BỘ + KÍCH HOẠT SSL TIÊU CHUẨN RENDER
private string GetSafeConnectionString()
{
    return "Server=dpg-d87m9p67r5hc738ph9u0-a.singapore-postgres.render.com;Database=coffeehousedb;Port=5432;User Id=coffeehousedb_user;Password=g5EGgOlb4B0ro32QE8ZTS9rFilgcUBKM;SslMode=Require;Trust Server Certificate=true;";
}

        public async Task<IActionResult> Index(string search, string category, string priceRange, string tableId)
        {
            // --- 1. "BẮT" MÃ BÀN VÀ CẤT VÀO SESSION ---
            if (!string.IsNullOrEmpty(tableId))
            {
                HttpContext.Session.SetString("SittingTable", tableId);
                ViewBag.CurrentTable = tableId; 
            }
            else 
            {
                ViewBag.CurrentTable = HttpContext.Session.GetString("SittingTable");
            }

            // --- 2. PHẦN CODE LỌC SẢN PHẨM ---
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(category) && category != "Tất cả")
            {
                query = query.Where(p => p.Category == category);
            }

            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "under30":
                        query = query.Where(p => p.Price < 30000);
                        break;
                    case "30to50":
                        query = query.Where(p => p.Price >= 30000 && p.Price <= 50000);
                        break;
                    case "above50":
                        query = query.Where(p => p.Price > 50000);
                        break;
                }
            }

            var products = await query.ToListAsync();

            // --- 3. BỐC THỐNG KÊ SAO TỪ DATABASE BẰNG ADO.NET ---
            var reviewStats = new Dictionary<int, (double Avg, int Count)>();
            string connString = GetSafeConnectionString(); // Gọi chuỗi kết nối VIP bảo mật
            
            using (NpgsqlConnection conn = new NpgsqlConnection(connString))
            {
                string sqlStats = @"SELECT ""ProductId"", AVG(CAST(""Rating"" AS DECIMAL(18,1))) AS AvgRating, COUNT(*) AS ReviewCount 
                                    FROM ""ProductReviews"" 
                                    GROUP BY ""ProductId""";
                using (NpgsqlCommand cmdStats = new NpgsqlCommand(sqlStats, conn))
                {
                    conn.Open();
                    using (var rdrStats = cmdStats.ExecuteReader())
                    {
                        while (rdrStats.Read())
                        {
                            int pid = Convert.ToInt32(rdrStats["ProductId"]);
                            double avg = Convert.ToDouble(rdrStats["AvgRating"]);
                            int count = Convert.ToInt32(rdrStats["ReviewCount"]);
                            reviewStats[pid] = (avg, count);
                        }
                    }
                }
            }

            ViewBag.ReviewStats = reviewStats;
            return View(products);
        }

        // --- 4. BỔ SUNG LẤY REVIEW SẢN PHẨM ---
        [HttpGet]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            var reviews = new List<object>();
            string connString = GetSafeConnectionString(); // Gọi chuỗi kết nối VIP bảo mật
            
            try {
                using (NpgsqlConnection conn = new NpgsqlConnection(connString)) {
                    string sql = @"SELECT ""CustomerName"", ""Rating"", ""Comment"", ""CreatedAt"" 
                                   FROM ""ProductReviews"" 
                                   WHERE ""ProductId"" = @pid 
                                   ORDER BY ""CreatedAt"" DESC";
                                   
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn)) {
                        cmd.Parameters.AddWithValue("@pid", productId);
                        await conn.OpenAsync();
                        
                        using (var reader = await cmd.ExecuteReaderAsync()) {
                            while (await reader.ReadAsync()) {
                                reviews.Add(new {
                                    customer = reader["CustomerName"].ToString(),
                                    rating = Convert.ToInt32(reader["Rating"]),
                                    comment = reader["Comment"] != DBNull.Value ? reader["Comment"].ToString() : "",
                                    date = ((DateTime)reader["CreatedAt"]).ToString("dd/MM/yyyy HH:mm")
                                });
                            }
                        }
                    }
                }
                return Json(reviews);
            } catch (Exception ex) {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}