using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoffeeHouseAdmin.Controllers
{
    public class AccountController : Controller
    {
        // Chuỗi kết nối Database của Boss - GIỮ NGUYÊN
        private readonly string _connectionString = @"Server=.;Database=CoffeeHouseDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        // ==========================================
        // 1. TRANG ĐĂNG NHẬP (HÀM GET - BỔ SUNG ĐỂ CHẠY ĐƯỢC LINK)
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ==========================================
        // 2. XỬ LÝ ĐĂNG NHẬP (HTTPPOST)
        // ==========================================
     [HttpPost]
public IActionResult Login(string Email, string Password)
{
    if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
    {
        TempData["Error"] = "Boss ơi, đừng để trống Email hoặc Mật khẩu nhé! 🥰";
        return View(); 
    }

   try
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            // 1. PHẢI THÊM CỘT Id VÀO CÂU LỆNH SELECT
            string sql = "SELECT Id, FullName, Email, Role FROM Customers WHERE Email = @e AND Password = @p";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@e", (object)Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p", (object)Password ?? DBNull.Value);

            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    // 2. LẤY Id RA TỪ DATABASE
                    int userId = Convert.ToInt32(reader["Id"]);
                    string name = reader["FullName"].ToString();
                    string email = reader["Email"].ToString();
                    string role = reader["Role"].ToString(); 

                    // 3. CẤT CẢ Id VÀ UserName VÀO SESSION
                    HttpContext.Session.SetInt32("UserId", userId); // DÒNG NÀY LÀ CHÌA KHÓA NÈ!
                    HttpContext.Session.SetString("UserName", name);
                    HttpContext.Session.SetString("UserEmail", email);
                    HttpContext.Session.SetString("UserRole", role); 

                    TempData["Success"] = $"Chào mừng {name} đã quay trở lại! 🥰";

                    if (role == "Admin") return RedirectToAction("Index", "Home", new { area = "Admin" });
                    return RedirectToAction("Index", "Home");
                }
            }
        }
        TempData["Error"] = "Sai tài khoản hoặc mật khẩu rồi Boss ơi! 🥺";
        return View();
    }
    catch (Exception ex) { return Content("Lỗi SQL: " + ex.Message); }

}

        // ==========================================
        // 3. ĐĂNG KÝ TRUYỀN THỐNG
        // ==========================================
        [HttpPost]
        public IActionResult Register(string FullName, string Email, string Phone, string Password, string ConfirmPassword)
        {
            if (Password != ConfirmPassword) {
                TempData["Error"] = "Mật khẩu không khớp Bạn ơi! 🥺";
                return RedirectToAction("Index", "Home");
            }

            try {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string sql = "INSERT INTO Customers (FullName, Email, Phone, Password) VALUES (@n, @e, @p, @pw)";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@n", FullName);
                    cmd.Parameters.AddWithValue("@e", Email);
                    cmd.Parameters.AddWithValue("@p", Phone ?? ""); 
                    cmd.Parameters.AddWithValue("@pw", Password);
                    
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Success"] = "Đăng ký thành công! Mời Bạn vào uống cafe 🥰";
            }
            catch (SqlException ex) {
                if (ex.Number == 2627 || ex.Number == 2601) 
                    TempData["Error"] = "Email này đã có người dùng rồi Bạn ạ!";
                else 
                    TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 4. ĐĂNG NHẬP GOOGLE / FACEBOOK
        // ==========================================
     public async Task<IActionResult> ExternalCallback() {
    var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!result.Succeeded) return RedirectToAction("Index", "Home");

    var fullName = result.Principal.FindFirstValue(ClaimTypes.Name);
    var email = result.Principal.FindFirstValue(ClaimTypes.Email);

    int idFromDb; // Biến tạm để giữ Id

    using (SqlConnection conn = new SqlConnection(_connectionString)) {
        conn.Open();
        // Kiểm tra và lấy Id của khách
        string checkSql = "SELECT Id FROM Customers WHERE Email = @e";
        SqlCommand checkCmd = new SqlCommand(checkSql, conn);
        checkCmd.Parameters.AddWithValue("@e", email);
        var existingId = checkCmd.ExecuteScalar();

        if (existingId == null) {
            // Nếu khách mới, tạo xong lấy Id vừa tạo luôn
            string ins = "INSERT INTO Customers (FullName, Email, Password, Role) VALUES (@n, @e, 'Social', 'User'); SELECT SCOPE_IDENTITY();";
            SqlCommand insCmd = new SqlCommand(ins, conn);
            insCmd.Parameters.AddWithValue("@n", fullName);
            insCmd.Parameters.AddWithValue("@e", email);
            idFromDb = Convert.ToInt32(insCmd.ExecuteScalar());
        } else {
            idFromDb = Convert.ToInt32(existingId);
        }
    }

    // CẤT VÀO SESSION CHO ĐỒNG BỘ
    HttpContext.Session.SetInt32("UserId", idFromDb); 
    HttpContext.Session.SetString("UserName", fullName);
    HttpContext.Session.SetString("UserEmail", email);
    
    return RedirectToAction("Index", "Home");
}
        // ==========================================
        // 5. ĐĂNG XUẤT
        // ==========================================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 6. PROFILE & HẠNG THÀNH VIÊN (GIỮ NGUYÊN LOGIC CỦA BOSS)
        // ==========================================
        public IActionResult Profile()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Index", "Home");

            decimal totalSpent = 0;
            var orders = new List<dynamic>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                
                string sumSql = "SELECT SUM(TotalAmount) FROM Orders WHERE CustomerName = @n AND Status != N'Đã hủy'";
                SqlCommand sumCmd = new SqlCommand(sumSql, conn);
                sumCmd.Parameters.AddWithValue("@n", userName);
                var sumResult = sumCmd.ExecuteScalar();
                totalSpent = sumResult != DBNull.Value ? Convert.ToDecimal(sumResult) : 0;

                string orderSql = "SELECT OrderId, CreatedAt, TotalAmount, Status FROM Orders WHERE CustomerName = @n ORDER BY CreatedAt DESC";
                SqlCommand orderCmd = new SqlCommand(orderSql, conn);
                orderCmd.Parameters.AddWithValue("@n", userName);
                using (var reader = orderCmd.ExecuteReader()) {
                    while (reader.Read()) {
                        orders.Add(new {
                            Id = reader["OrderId"],
                            OrderDate = (DateTime)reader["CreatedAt"],
                            TotalPrice = Convert.ToDecimal(reader["TotalAmount"]),
                            Status = reader["Status"].ToString()
                        });
                    }
                }
            }

            string rankName = "Thành viên Mới 🌱";
            string rankColor = "text-muted";

            if (userRole == "Admin") {
                rankName = "Quản trị viên 👑";
                rankColor = "text-danger";
            }
            else if (totalSpent >= 5000000) {
                rankName = "Thành viên Vàng 🏆";
                rankColor = "text-warning"; 
            }
            else if (totalSpent >= 1000000) {
                rankName = "Thành viên Bạc ✨";
                rankColor = "text-secondary"; 
            }
            else if (totalSpent > 0) {
                rankName = "Thành viên Đồng ☕";
                rankColor = "text-bronze"; 
            }

            ViewBag.FullName = userName;
            ViewBag.Email = userEmail;
            ViewBag.RankName = rankName;
            ViewBag.RankColor = rankColor;
            ViewBag.TotalSpent = totalSpent;

            return View(orders);
        }
    }
}