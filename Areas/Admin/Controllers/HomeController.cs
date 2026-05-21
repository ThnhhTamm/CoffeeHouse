using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace CoffeeHouseAdmin.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly IConfiguration _conf;
        public HomeController(IConfiguration conf) { _conf = conf; }

       public IActionResult Index()
{
    // Khởi tạo danh sách chứa sản phẩm bán chạy
    var topProducts = new List<dynamic>();

    using (SqlConnection conn = new SqlConnection(_conf.GetConnectionString("DefaultConnection")))
    {
        conn.Open();

        // 1. Lấy 4 con số tổng quát (Doanh thu, Đơn hàng, Khách, AI)
        ViewBag.TotalRevenue = GetScalar(conn, "SELECT SUM(TotalAmount) FROM Orders");
        ViewBag.TotalOrders = GetScalar(conn, "SELECT COUNT(*) FROM Orders");
        ViewBag.TotalCustomers = GetScalar(conn, "SELECT COUNT(*) FROM Customers");
        ViewBag.TotalAI = GetScalar(conn, "SELECT COUNT(*) FROM ChatLogs");

        // 2. Con số hôm nay (Sản phẩm, Đơn hôm nay, Chat hôm nay)
        ViewBag.ProductsCount = GetScalar(conn, "SELECT COUNT(*) FROM Products");
        ViewBag.OrdersToday = GetScalar(conn, "SELECT COUNT(*) FROM Orders WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)");
        ViewBag.AIChatToday = GetScalar(conn, "SELECT COUNT(*) FROM ChatLogs WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)");

        // 3. Lấy dữ liệu Biểu đồ (7 ngày gần nhất)
        var chartLabels = new List<string>();
        var chartValues = new List<decimal>();
        string sqlChart = @"SELECT TOP 7 CAST(CreatedAt AS DATE) as Ngay, SUM(TotalAmount) as DoanhThu 
                            FROM Orders GROUP BY CAST(CreatedAt AS DATE) ORDER BY Ngay DESC";

        using (var cmdChart = new SqlCommand(sqlChart, conn))
        using (var rdrChart = cmdChart.ExecuteReader()) {
            while (rdrChart.Read()) {
                chartLabels.Insert(0, ((DateTime)rdrChart["Ngay"]).ToString("dd/MM"));
                chartValues.Insert(0, (decimal)rdrChart["DoanhThu"] / 1000000);
            }
        }
        ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(chartLabels);
        ViewBag.ChartValues = System.Text.Json.JsonSerializer.Serialize(chartValues);

        // 4. LẤY TOP 5 SẢN PHẨM BÁN CHẠY (Dùng 'conn' ở đây mới đúng nè Boss)
        string sqlTopProducts = @"
            SELECT TOP 5 ProductName, SUM(Quantity) as TotalQty, SUM(Quantity * Price) as TotalRev
            FROM OrderItems
            GROUP BY ProductName
            ORDER BY TotalQty DESC";

        using (var cmdTop = new SqlCommand(sqlTopProducts, conn))
        using (var rdrTop = cmdTop.ExecuteReader()) {
            while (rdrTop.Read()) {
                topProducts.Add(new {
                    Name = rdrTop["ProductName"].ToString(),
                    Qty = rdrTop["TotalQty"].ToString() + " ly",
                    Rev = string.Format("{0:N0}k", (decimal)rdrTop["TotalRev"] / 1000)
                });
            }
        }
    } // Kết thúc using conn - Biến 'conn' sẽ biến mất sau dấu ngoặc này

    ViewBag.TopProducts = topProducts;
    return View();
}

        private object GetScalar(SqlConnection conn, string sql) {
            var cmd = new SqlCommand(sql, conn);
            var res = cmd.ExecuteScalar();
            return res == DBNull.Value || res == null ? 0 : res;
        }
    }
}