using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoffeeHouseAdmin.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReviewsController : Controller
    {
        private readonly string _connectionString = @"Server=.;Database=CoffeeHouseDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        // 1. TRANG HIỂN THỊ DANH SÁCH ĐÁNH GIÁ
        public async Task<IActionResult> Index()
        {
            var reviews = new List<dynamic>();
            // Nối bảng Products để lấy tên món ăn
            string sql = @"SELECT r.ReviewId, p.Name AS ProductName, r.CustomerName, r.Rating, r.Comment, r.CreatedAt 
                           FROM ProductReviews r
                           JOIN Products p ON r.ProductId = p.Id
                           ORDER BY r.CreatedAt DESC";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            reviews.Add(new {
                                Id = (int)reader["ReviewId"],
                                ProductName = reader["ProductName"].ToString(),
                                Customer = reader["CustomerName"].ToString(),
                                Stars = (int)reader["Rating"],
                                Comment = reader["Comment"] != DBNull.Value ? reader["Comment"].ToString() : "",
                                Date = (DateTime)reader["CreatedAt"]
                            });
                        }
                    }
                }
            }
            return View(reviews);
        }

        // 2. TÍNH NĂNG XÓA ĐÁNH GIÁ SPAM (Rất cần cho trang Admin)
        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "DELETE FROM ProductReviews WHERE ReviewId = @id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            return RedirectToAction("Index");
        }
    }
}