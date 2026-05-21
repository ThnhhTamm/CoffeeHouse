using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Gỡ lỗi đỏ cho SqlConnection, SqlCommand
using CoffeeHouseAdmin.Models; // Gỡ lỗi đỏ cho Promotion

[Area("Admin")]
public class PromotionsController : Controller
{
    private readonly string _connectionString = "Server=.;Database=CoffeeHouseDB;Trusted_Connection=True;TrustServerCertificate=True;";
    // 1. Trang danh sách mã giảm giá
    public async Task<IActionResult> Index()
    {
        List<Promotion> promos = new List<Promotion>();
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            string sql = "SELECT * FROM Promotions ORDER BY ExpiryDate DESC";
            await conn.OpenAsync();
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        promos.Add(new Promotion {
                            PromoId = (int)reader["PromoId"],
                            PromoCode = reader["PromoCode"].ToString(),
                            DiscountPercent = (int)reader["DiscountPercent"],
                            DiscountAmount = (decimal)reader["DiscountAmount"],
                            ExpiryDate = (DateTime)reader["ExpiryDate"],
                            IsActive = (bool)reader["IsActive"]
                        });
                    }
                }
            }
        }
        return View(promos);
    }

    // 2. Hàm thêm mã mới (Post)
    [HttpPost]
    public async Task<IActionResult> Create(string code, int percent, decimal amount, decimal minOrder, DateTime expiry)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            string sql = @"INSERT INTO Promotions (PromoCode, DiscountPercent, DiscountAmount, MinOrderAmount, ExpiryDate, IsActive) 
                           VALUES (@code, @percent, @amount, @min, @expiry, 1)";
            await conn.OpenAsync();
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@code", code.ToUpper());
                cmd.Parameters.AddWithValue("@percent", percent);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@min", minOrder);
                cmd.Parameters.AddWithValue("@expiry", expiry);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        return RedirectToAction("Index");
    }
    // 3. Hàm Khóa/Mở trạng thái mã
[HttpPost]
public async Task<IActionResult> ToggleStatus(int id)
{
    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        // Câu lệnh SQL thông minh: Tự động đảo trạng thái 1 -> 0 hoặc 0 -> 1
        string sql = "UPDATE Promotions SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE PromoId = @id";
        await conn.OpenAsync();
        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
    return RedirectToAction("Index");
}
}