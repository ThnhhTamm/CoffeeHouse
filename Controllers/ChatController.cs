using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient; 

namespace CoffeeHouseAdmin.Controllers
{
    public class ChatController : Controller
    {
        private readonly string _geminiApiKey = "AIzaSyDt_y7_n5NLEi3J4y4-MPr5iUij2SJzNrY";
        private readonly string _connectionString = @"Server=.;Database=CoffeeHouseDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

       [HttpPost]
public async Task<IActionResult> AskGemini([FromBody] JsonElement request)
{
    try
    {
        var userMessage = request.GetProperty("message").GetString();

        string menuData = "";
        if (request.TryGetProperty("menu", out var menuProp))
        {
            menuData = menuProp.GetString();
        }
        else
        {
            menuData = "Cà phê sữa đá: 29.000đ, Bạc xỉu: 35.000đ"; 
        }

        // --- BƯỚC MỚI: TỰ ĐỘNG BỐC KHUYẾN MÃI THỜI GIAN THỰC TỪ DATABASE ---
        string voucherData = await GetFreshVouchersFromDbAsync();
        // -----------------------------------------------------------------

        var historyKey = "ChatHistory";
        var historyJson = HttpContext.Session.GetString(historyKey) ?? "[]";
        var history = JsonSerializer.Deserialize<List<object>>(historyJson);

        history.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

        using var client = new HttpClient();
        var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite-preview:generateContent?key={_geminiApiKey.Trim()}";
        
        // 1. Lấy mã bàn từ Session
        var tableId = HttpContext.Session.GetString("SittingTable");
        var contextContext = string.IsNullOrEmpty(tableId) 
            ? "Khách đang đặt ONLINE. Khi khách chốt đơn, hãy trả lời xác nhận và LUÔN kèm thẻ [CHECKOUT] để dắt khách đi thanh toán ngay."
            : $"Khách đang ngồi tại BÀN {tableId}. Khi khách chốt món xong, hãy trả lời và kèm thẻ [CHECKOUT] để dẫn khách ra xem lại Giỏ hàng.";

        // 2. Chèn vào System Prompt (Đã tích hợp thêm biến {voucherData} của Boss)
        var systemPrompt = $@"
            Bạn là Bé Phin AI - nhân viên lon ton, siêu cute tại Coffee House.
            Dữ liệu Menu: {menuData}.
            {voucherData}
            {contextContext}

            PHONG CÁCH: Trả lời cực nhanh, thông minh, xưng 'em' gọi 'Bạn'. Giọng văn ngọt ngào, dùng icon 🥰, ✨.
            QUY TẮC: Tuyệt đối KHÔNG dùng dấu * hoặc số thứ tự đầu dòng.
            
            ĐỊNH DẠNG HTML:
            - Các cụm từ PHẢI được bôi đậm và đổi màu nâu bằng thẻ <b style='color:#5c4033'>...</b> :
              + Tên của em: 'Bé Phin'
              + Tên quán: 'Coffee House'
              + Tên các món đồ uống và giá tiền (Ví dụ: <b style='color:#5c4033'>Bạc xỉu: 35.000đ</b>)
            - Định dạng tiền: Phải có dấu chấm (Ví dụ: 35.000đ).
            - Liệt kê hàng dọc, bắt đầu bằng icon: ☕, 🥤 hoặc 🍰.

            NHIỆM VỤ ĐẶC BIỆT (GỌI MÓN NGẦM):
            - Nếu khách muốn gọi món, sau câu trả lời ngọt ngào, hãy thêm thẻ ẩn: [ORDER: Tên món | Giá tiền].
            - QUY TẮC CỨNG: Khi khách nói 'thanh toán', 'tính tiền', 'mua luôn', em PHẢI trả lời xác nhận và BẮT BUỘC viết thẻ [CHECKOUT] ở cuối cùng của câu trả lời. KHÔNG ĐƯỢC QUÊN, đây là lệnh quan trọng nhất!   
            - Khi khách gọi nhiều món, hãy ghi thẻ [ORDER: ...] ngay sau mỗi món đó. Để đảm bảo AI trả về tin nhắn có dạng:

            Món A [ORDER: A | Giá]

            Món B [ORDER: B | Giá]

            Món C [ORDER: C | Giá]
            - Ví dụ: 'Dạ có ngay ạ! [ORDER: Cà phê muối | 29.000đ]'";

        var payload = new
        {
            contents = new List<object> {
                new { role = "user", parts = new[] { new { text = systemPrompt } } },
                new { role = "model", parts = new[] { new { text = "Dạ em đã rõ quy tắc ạ! 🥰" } } }
            }
        };
        ((List<object>)payload.contents).AddRange(history);

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(apiUrl, content);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return Json(new { reply = $"Huhu Boss ơi, Gemini 3.1 báo lỗi {response.StatusCode}: {result}" });
        }

        using var doc = JsonDocument.Parse(result);
        string reply = doc.RootElement.GetProperty("candidates")[0]
                                       .GetProperty("content")
                                       .GetProperty("parts")[0]
                                       .GetProperty("text").GetString();

        string finalReply = reply.Replace("*", "").Trim(); 

        // --- ĐOẠN LƯU LOG ĐÃ ĐƯỢC CẬP NHẬT ĐỂ HẾT LỖI ---
        string userName = HttpContext.Session.GetString("UserName") ?? "Khách vãng lai";
        SaveChatLogToDatabase(userName, userMessage, finalReply);

        history.Add(new { role = "model", parts = new[] { new { text = finalReply } } });
        HttpContext.Session.SetString(historyKey, JsonSerializer.Serialize(history));

        return Json(new { reply = finalReply });
    }
    catch (Exception ex)
    {
        return Json(new { reply = "Lỗi hệ thống rồi Boss ơi: " + ex.Message });
    }
}

private async Task<string> GetFreshVouchersFromDbAsync()
{
    string voucherContext = "DỮ LIỆU KHUYẾN MÃI (Hãy tư vấn cho khách nếu họ hỏi nhé):\n";
    bool hasVoucher = false;

    try
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            // Quét bảng Promotions lấy các mã đang chạy và còn hạn sử dụng
            string sql = @"SELECT PromoCode, DiscountPercent, DiscountAmount, MinOrderAmount 
                           FROM Promotions 
                           WHERE IsActive = 1 AND ExpiryDate >= GETDATE()";

            SqlCommand cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    hasVoucher = true;
                    string code = reader["PromoCode"].ToString();
                    int percent = Convert.ToInt32(reader["DiscountPercent"]);
                    decimal amount = Convert.ToDecimal(reader["DiscountAmount"]);
                    decimal minOrder = Convert.ToDecimal(reader["MinOrderAmount"]);

                    if (percent > 0) {
                        voucherContext += $"- Mã {code}: Giảm {percent}% cho đơn từ {minOrder:N0}đ.\n";
                    } else {
                        voucherContext += $"- Mã {code}: Giảm {amount:N0}đ cho đơn từ {minOrder:N0}đ.\n";
                    }
                }
            }
        }
    }
    catch
    {
        // Nếu DB lỗi thì coi như không có voucher để không làm sập Chatbot
        hasVoucher = false;
    }

    if (!hasVoucher)
    {
        voucherContext = "Hiện tại quán đang tạm thời hết chương trình khuyến mãi.\n";
    }

    return voucherContext;
}

        private void SaveChatLogToDatabase(string name, string userMsg, string aiReply)
        {
            try
            {
                string intent = "Hỏi đáp";
                if (userMsg.ToLower().Contains("menu") || userMsg.ToLower().Contains("giá")) intent = "Hỏi Menu";
                else if (userMsg.ToLower().Contains("đặt bàn")) intent = "Đặt bàn";
                else if (userMsg.ToLower().Contains("chậm") || userMsg.ToLower().Contains("tệ")) intent = "Khiếu nại";

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // --- CẬP NHẬT: Dùng CreatedAt thay vì Timestamp, bỏ LogID để SQL tự tăng Id ---
                    string sql = @"INSERT INTO ChatLogs (CustomerName, UserMessage, AIResponse, CreatedAt, Intent, Status) 
                                   VALUES (@name, @msg, @reply, GETDATE(), @intent, N'Đã giải quyết')";
                    
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@msg", userMsg);
                    cmd.Parameters.AddWithValue("@reply", aiReply);
                    cmd.Parameters.AddWithValue("@intent", intent);
                    
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* Im lặng để khách không bị lỗi khi chat */ }
        }
    }
}