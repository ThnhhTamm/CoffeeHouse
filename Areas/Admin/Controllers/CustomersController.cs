using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CoffeeHouseAdmin.Models;
using System.Data;

namespace CoffeeHouseAdmin.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CustomersController : Controller
    {
        // 1. Chuỗi kết nối "xương sống"
        private readonly string _connectionString = @"Server=.;Database=CoffeeHouseDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        // ==========================================
        // 1. TRANG DANH SÁCH (INDEX)
        // ==========================================
        public IActionResult Index(string search)
        {
            List<Customer> customers = new List<Customer>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // SQL lồng: Lấy khách + tính tổng đơn & tiền ngay tại chỗ
                string sql = @"
                    SELECT c.*, 
                    (SELECT COUNT(*) FROM Orders o WHERE o.CustomerName = c.FullName) as TotalOrders,
                    (SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders o WHERE o.CustomerName = c.FullName AND o.Status != N'Đã hủy') as TotalSpent
                    FROM Customers c 
                    WHERE c.Role = 'Customer'";

                if (!string.IsNullOrEmpty(search)) {
                    sql += " AND (c.FullName LIKE @s OR c.Phone LIKE @s)";
                }
                sql += " ORDER BY c.Id DESC";

                SqlCommand cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@s", "%" + search + "%");

                conn.Open();
               using (var reader = cmd.ExecuteReader()) {
                  // Tìm chỗ lặp reader.Read() của danh sách khách hàng và sửa lại:
while (reader.Read())
{
    customers.Add(new Customer {
    // 1. Sửa 'Id' thành 'CustomerID' cho đúng với Model
    // 2. Vì Model là 'string' nên mình dùng .ToString() nhé Boss
    CustomerID = reader["Id"].ToString(), 

    FullName = reader["FullName"].ToString(),
    Email = reader["Email"]?.ToString(),
    Phone = reader["Phone"]?.ToString(),

    // Các dòng dưới giữ nguyên vì kiểu dữ liệu đã khớp (int và decimal)
    TotalOrders = reader["TotalOrders"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalOrders"]),
    TotalSpent = reader["TotalSpent"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalSpent"]),
    Rank = reader["Rank"]?.ToString() ?? "Thành viên"
});
}
                }
            }
            ViewBag.SearchTerm = search;
            return View(customers);
        }

     public IActionResult Details(string id)
{
    int realId = int.Parse(id.Replace("KH", ""));
    Customer customer = null;
    List<Order> orderHistory = new List<Order>();
    var favoriteProducts = new Dictionary<string, int>();

    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        conn.Open();

        // --- A. Lấy thông tin cơ bản của khách ---
        string sql = "SELECT * FROM Customers WHERE Id = @id";
        SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", realId);
        using (var reader = cmd.ExecuteReader()) {
            if (reader.Read()) {
                customer = new Customer {
                    CustomerID = id,
                    FullName = reader["FullName"].ToString(),
                    Email = reader["Email"]?.ToString() ?? "",
                    Phone = reader["Phone"]?.ToString() ?? ""
                };
            }
        }

        if (customer == null) return RedirectToAction("Index");

        // --- B. Lấy lịch sử đơn hàng thật ---
        string orderSql = "SELECT OrderId, CreatedAt, TotalAmount, Status FROM Orders WHERE CustomerName = @n ORDER BY CreatedAt DESC";
        SqlCommand oCmd = new SqlCommand(orderSql, conn);
        oCmd.Parameters.AddWithValue("@n", customer.FullName);
        using (var r = oCmd.ExecuteReader()) {
            while (r.Read()) {
                orderHistory.Add(new Order {
                    OrderID = r["OrderId"].ToString(),
                    OrderDate = (DateTime)r["CreatedAt"],
                    TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                    Status = r["Status"].ToString()
                });
            }
        }

        // --- C. TRUY TÌM 'MÓN TỦ' TỪ DATABASE (Phần mới cho Boss nè) ---
        // Lưu ý: Boss cần bảng OrderItems lưu chi tiết món mới chạy được nhé
        string favSql = @"
            SELECT TOP 3 ProductName, SUM(Quantity) as TotalQty
            FROM OrderItems 
            WHERE OrderId IN (SELECT OrderId FROM Orders WHERE CustomerName = @name AND Status != N'Đã hủy')
            GROUP BY ProductName
            ORDER BY TotalQty DESC";

        SqlCommand favCmd = new SqlCommand(favSql, conn);
        favCmd.Parameters.AddWithValue("@name", customer.FullName);
        
        using (var reader = favCmd.ExecuteReader()) {
            while (reader.Read()) {
                favoriteProducts.Add(
                    reader["ProductName"].ToString(), 
                    Convert.ToInt32(reader["TotalQty"])
                );
            }
        }
    }

    // --- Đổ dữ liệu thật vào túi ---
    ViewBag.OrderHistory = orderHistory;
    ViewBag.FavoriteProducts = favoriteProducts; // Bây giờ là dữ liệu thật rồi Boss ơi!

    // Tính toán thêm 1 chút cho "xịn"
    customer.TotalSpent = orderHistory.Where(o => o.Status != "Đã hủy").Sum(o => o.TotalAmount);
    customer.TotalOrders = orderHistory.Count;
    customer.Rank = customer.TotalSpent >= 1000000 ? "VIP Vàng" : "Thành viên";

    return View(customer);
}

        // ==========================================
        // 3. LƯU CHỈNH SỬA (EDIT)
        // ==========================================
        [HttpPost]
        public IActionResult Edit(Customer updatedCustomer)
        {
            int realId = int.Parse(updatedCustomer.CustomerID.Replace("KH", ""));

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE Customers SET FullName = @n, Phone = @p, Email = @e WHERE Id = @id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@n", updatedCustomer.FullName);
                cmd.Parameters.AddWithValue("@p", updatedCustomer.Phone ?? "");
                cmd.Parameters.AddWithValue("@e", updatedCustomer.Email);
                cmd.Parameters.AddWithValue("@id", realId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Details", new { id = updatedCustomer.CustomerID });
        }
    }
}