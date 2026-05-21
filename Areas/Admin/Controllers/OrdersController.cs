using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CoffeeHouseAdmin.Models;
using System.Data;

namespace CoffeeHouseAdmin.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrdersController : Controller
    {
        private readonly string _connectionString = @"Server=.;Database=CoffeeHouseDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        // ==========================================
        // 1. TRANG DANH SÁCH & LỌC (INDEX)
        // ==========================================
        public IActionResult Index(string search, string status)
        {
            List<Order> orders = new List<Order>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // --- A. LẤY DANH SÁCH ĐƠN HÀNG ---
                // Bé Phin dùng SELECT * để lấy hết, nhưng quan trọng là đoạn gán dữ liệu bên dưới
                string sql = "SELECT * FROM Orders WHERE 1=1";
                
                if (!string.IsNullOrEmpty(search)) {
                    sql += " AND (OrderID LIKE @s OR CustomerName LIKE @s OR Phone LIKE @s OR TableID LIKE @s)";
                }
                if (!string.IsNullOrEmpty(status) && status != "Tất cả trạng thái") {
                    sql += " AND Status = @st";
                }
                sql += " ORDER BY OrderDate DESC"; // Hiện đơn mới nhất lên đầu

                SqlCommand cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@s", "%" + search + "%");
                if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("@st", status);

                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        orders.Add(new Order {
                            OrderID = reader["OrderID"].ToString(),
                            CustomerName = reader["CustomerName"].ToString(),
                            Phone = reader["Phone"]?.ToString() ?? "",
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            Status = reader["Status"].ToString(),
                            OrderDate = (DateTime)reader["OrderDate"],
                            
                            // --- BOSS CHÚ Ý 3 DÒNG "VÀNG NGỌC" NÀY NHÉ ---
                            TableID = reader["TableID"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            ShippingAddress = reader["ShippingAddress"]?.ToString()
                        });
                    }
                }

                // --- B. ĐẾM SỐ LƯỢNG CHO DASHBOARD ---
                ViewBag.Pending = GetCount(conn, "Chờ xác nhận");
                ViewBag.Confirmed = GetCount(conn, "Đã xác nhận");
                ViewBag.Preparing = GetCount(conn, "Đang chuẩn bị");
                ViewBag.Delivering = GetCount(conn, "Đang giao");
                ViewBag.Delivered = GetCount(conn, "Đã giao");
                ViewBag.Cancelled = GetCount(conn, "Đã hủy");
            }

            ViewBag.SearchTerm = search;
            ViewBag.CurrentStatus = status;

            return View(orders);
        }

        // ==========================================
        // 2. XEM CHI TIẾT ĐƠN HÀNG (DETAILS)
        // ==========================================
        public IActionResult Details(string id)
        {
            Order orderHeader = null;
            List<dynamic> orderItems = new List<dynamic>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 1. Lấy thông tin chung (Cập nhật lấy đầy đủ cột mới)
                string sqlOrder = "SELECT * FROM Orders WHERE OrderID = @id";
                SqlCommand cmd1 = new SqlCommand(sqlOrder, conn);
                cmd1.Parameters.AddWithValue("@id", id);
                using (var reader = cmd1.ExecuteReader()) {
                    if (reader.Read()) {
                        orderHeader = new Order {
                            OrderID = reader["OrderID"].ToString(),
                            CustomerName = reader["CustomerName"].ToString(),
                            Phone = reader["Phone"]?.ToString() ?? "N/A",
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            Status = reader["Status"].ToString(),
                            OrderDate = (DateTime)reader["OrderDate"],
                            TableID = reader["TableID"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            ShippingAddress = reader["ShippingAddress"]?.ToString()
                        };
                    }
                }

                if (orderHeader == null) return RedirectToAction("Index");

                // 2. Lấy danh sách món ăn
                string sqlItems = "SELECT * FROM OrderItems WHERE OrderId = @id";
                SqlCommand cmd2 = new SqlCommand(sqlItems, conn);
                cmd2.Parameters.AddWithValue("@id", id);
                using (var reader = cmd2.ExecuteReader()) {
                    while (reader.Read()) {
                        orderItems.Add(new {
                            Product = reader["ProductName"].ToString(),
                            Qty = (int)reader["Quantity"],
                            Price = Convert.ToDecimal(reader["Price"])
                        });
                    }
                }
            }

            ViewBag.Items = orderItems;
            return View(orderHeader); // Trả về object Order thay vì dynamic cho nó "chuẩn bài"
        }

        // --- HÀM PHỤ TRỢ ĐẾM ĐƠN ---
        private int GetCount(SqlConnection conn, string statusName) {
            string sql = "SELECT COUNT(*) FROM Orders WHERE Status = @s";
            using (SqlCommand cmd = new SqlCommand(sql, conn)) {
                cmd.Parameters.AddWithValue("@s", statusName);
                return (int)cmd.ExecuteScalar();
            }
        }

        // ==========================================
        // 3. CẬP NHẬT TRẠNG THÁI (UPDATE STATUS)
        // ==========================================
        [HttpPost]
        public IActionResult UpdateStatus(string orderId, string newStatus)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE Orders SET Status = @s WHERE OrderID = @id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@s", newStatus);
                cmd.Parameters.AddWithValue("@id", orderId);
                
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }
    }
}