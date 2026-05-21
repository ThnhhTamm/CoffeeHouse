using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; 
using CoffeeHouseAdmin.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CoffeeHouseAdmin.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ChatLogsController : Controller
    {
        private readonly string _connectionString = @"Server=.;Database=CoffeeHouseDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        public IActionResult Index(string search)
        {
            List<ChatLog> logs = new List<ChatLog>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 1. Câu lệnh SQL - Đảm bảo lấy đúng CreatedAt
                string sql = "SELECT * FROM ChatLogs WHERE 1=1";
                if (!string.IsNullOrEmpty(search))
                {
                    sql += " AND (UserMessage LIKE @s OR CustomerName LIKE @s OR Intent LIKE @s)";
                }
                sql += " ORDER BY CreatedAt DESC"; 

                SqlCommand cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(search))
                {
                    cmd.Parameters.AddWithValue("@s", "%" + search + "%");
                }

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        // --- ĐOẠN XỬ LÝ AN TOÀN ---
                        var log = new ChatLog
                        {
                            Id = Convert.ToInt32(dr["Id"]), 
                            CustomerName = dr["CustomerName"]?.ToString() ?? "Khách ẩn danh",
                            UserMessage = dr["UserMessage"]?.ToString(),
                            AIResponse = dr["AIResponse"]?.ToString(),
                            CreatedAt = dr["CreatedAt"] != DBNull.Value ? (DateTime)dr["CreatedAt"] : DateTime.Now,
                            Intent = dr["Intent"]?.ToString() ?? "Hỏi đáp"
                        };

                        // Kiểm tra xem cột Status có tồn tại trong SQL chưa để tránh lỗi IndexOutOfRange
                        try {
                            log.Status = dr["Status"]?.ToString() ?? "Đã giải quyết";
                        } catch {
                            log.Status = "Đã giải quyết"; // Giá trị mặc định nếu SQL chưa có cột
                        }

                        logs.Add(log);
                    }
                }

                // 2. THỐNG KÊ NHANH (Dữ liệu thật)
                ViewBag.Total = logs.Count;
                ViewBag.NeedHelp = logs.Count(l => l.Status == "Cần hỗ trợ");
                ViewBag.Resolved = logs.Count(l => l.Status == "Đã giải quyết");
            }

            ViewBag.SearchTerm = search;
            return View(logs);
        }
    }
}