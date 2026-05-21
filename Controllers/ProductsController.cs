using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CoffeeHouseAdmin.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;

namespace CoffeeHouseAdmin.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IConfiguration _conf;

        public ProductsController(IConfiguration conf)
        {
            _conf = conf;
        }

        private string GetConn() => _conf.GetConnectionString("DefaultConnection");

        // 1. HIỂN THỊ DANH SÁCH & TÌM KIẾM
        public IActionResult Index(string search)
        {
            var list = new List<Product>();
            using (SqlConnection conn = new SqlConnection(GetConn()))
            {
                conn.Open();
                string sql = @"SELECT * FROM Products 
                               WHERE @s IS NULL 
                               OR Name LIKE '%' + @s + '%' 
                               OR Category LIKE '%' + @s + '%' 
                               ORDER BY Id DESC";

                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@s", (object)search ?? DBNull.Value);
                
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new Product
                    {
                        Id = (int)rdr["Id"],
                        Name = rdr["Name"].ToString() ?? "",
                        Price = (decimal)rdr["Price"],
                        Description = rdr["Description"]?.ToString(),
                        ImageUrl = rdr["ImageUrl"]?.ToString(),
                        Category = rdr["Category"]?.ToString(),
                        Stock = rdr["Stock"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["Stock"]),
                        RoastLevel = rdr["RoastLevel"]?.ToString(),
                        FlavorProfile = rdr["FlavorProfile"]?.ToString(),
                        
                        // FIX Ở ĐÂY: Dùng ToString() thay vì Convert.ToInt32
                        BitternessLevel = rdr["BitternessLevel"] == DBNull.Value ? null : rdr["BitternessLevel"].ToString(),

                        // ĐỌC THÊM 2 DỮ LIỆU SAO VỪA TÍNH ĐƯỢC DƯỚI DATABASE ĐẨY RA VIEW
                // AvgRating = Convert.ToDouble(rdr["AvgRating"]),
                // ReviewCount = (int)rdr["ReviewCount"]
                    });
                }
            }
            return View(list);
        }

        // 2. THÊM MÓN MỚI
        [HttpPost]
        public IActionResult Create(Product p)
        {
            using (SqlConnection conn = new SqlConnection(GetConn()))
            {
                string sql = @"INSERT INTO Products (Name, Price, Description, Category, ImageUrl, Stock, RoastLevel, BitternessLevel, FlavorProfile) 
                               VALUES (@n, @p, @d, @c, @i, @s, @r, @b, @f)";
                
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@n", (object)p.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", p.Price);
                cmd.Parameters.AddWithValue("@d", (object)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@c", (object)p.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@i", (object)p.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@s", p.Stock ?? 0);
                cmd.Parameters.AddWithValue("@r", (object)p.RoastLevel ?? DBNull.Value);
                
                // BitternessLevel giờ là string nên AddWithValue sẽ tự hiểu, cực an toàn
                cmd.Parameters.AddWithValue("@b", (object)p.BitternessLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@f", (object)p.FlavorProfile ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        // 3. CẬP NHẬT SẢN PHẨM
        [HttpPost]
        public IActionResult Edit(Product p)
        {
            using (SqlConnection conn = new SqlConnection(GetConn()))
            {
                string sql = @"UPDATE Products 
                               SET Name = @n, Price = @p, Description = @d, Category = @c, 
                                   ImageUrl = @i, Stock = @s, RoastLevel = @r, 
                                   BitternessLevel = @b, FlavorProfile = @f 
                               WHERE Id = @id";

                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.Parameters.AddWithValue("@n", (object)p.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", p.Price);
                cmd.Parameters.AddWithValue("@d", (object)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@c", (object)p.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@i", (object)p.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@s", p.Stock ?? 0);
                cmd.Parameters.AddWithValue("@r", (object)p.RoastLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@b", (object)p.BitternessLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@f", (object)p.FlavorProfile ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        // 4. XÓA MÓN (Giữ nguyên)
        public IActionResult Delete(int id)
        {
            using (SqlConnection conn = new SqlConnection(GetConn()))
            {
                var cmd = new SqlCommand("DELETE FROM Products WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }
       [HttpPost]
public async Task<IActionResult> SubmitReview(int productId, string orderId, string customerName, int rating, string comment)
{
    // Kiểm tra điều kiện đơn giản (Giữ nguyên của Boss)
    if (rating < 1 || string.IsNullOrEmpty(customerName)) 
        return Json(new { success = false, message = "Boss ơi, điền đủ tên và số sao nhé! 🥰" });

    try {
        using (SqlConnection conn = new SqlConnection(GetConn())) {
            await conn.OpenAsync();

            // 🌟 NÂNG CẤP BƯỚC 1: KIỂM TRA TRÙNG LẶP THEO TỪNG ĐƠN HÀNG BIỆT LẬP
            // Check xem người dùng này đã từng chấm điểm cho món này TRONG ĐƠN NÀY chưa
            string sqlCount = @"SELECT COUNT(*) FROM ProductReviews 
                               WHERE ProductId = @pid AND CustomerName = @cname AND OrderId = @oid";
            
            using (SqlCommand cmdCount = new SqlCommand(sqlCount, conn)) {
                cmdCount.Parameters.AddWithValue("@pid", productId);
                cmdCount.Parameters.AddWithValue("@cname", customerName);
                cmdCount.Parameters.AddWithValue("@oid", orderId ?? ""); // Mã đơn hàng truyền từ JS lên

                int count = (int)await cmdCount.ExecuteScalarAsync();
                if (count > 0)
                {
                    return Json(new { success = false, message = "Boss ơi, món nước này trong đơn hàng này Boss đã đánh giá rồi ạ! 🥰" });
                }
            }

            // 🌟 NÂNG CẤP BƯỚC 2: CẬP NHẬT CÂU LỆNH INSERT ĐỂ LƯU THÊM CỘT ORDERID
            string sql = @"INSERT INTO ProductReviews (ProductId, OrderId, CustomerName, Rating, Comment, CreatedAt) 
                           VALUES (@pid, @oid, @name, @star, @msg, GETDATE())";
                           
            using (SqlCommand cmd = new SqlCommand(sql, conn)) {
                cmd.Parameters.AddWithValue("@pid", productId);
                cmd.Parameters.AddWithValue("@oid", orderId ?? ""); // Bơm tham số mã đơn hàng vào đây để lưu xuống DB
                cmd.Parameters.AddWithValue("@name", customerName);
                cmd.Parameters.AddWithValue("@star", rating);
                cmd.Parameters.AddWithValue("@msg", comment ?? "");
                await cmd.ExecuteNonQueryAsync();
            }
        }
        return Json(new { success = true, message = "Cảm ơn Boss đã đánh giá ! ⭐⭐⭐⭐⭐" });
    } catch (Exception ex) {
        return Json(new { success = false, message = "Lỗi Database: " + ex.Message });
    }
}
    }
}