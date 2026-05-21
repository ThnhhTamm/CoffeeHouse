using System;

namespace CoffeeHouseAdmin.Models
{
    public class ChatLog
    {
        // Phải là Id (kiểu int) để khớp với cột Id tự tăng trong SQL
        public int Id { get; set; } 

        public string CustomerName { get; set; }

        public string UserMessage { get; set; }

        public string AIResponse { get; set; }

        // Phải là CreatedAt để khớp với ảnh cấu trúc bảng Boss chụp
        public DateTime CreatedAt { get; set; } 

        public string Intent { get; set; }

        public string Status { get; set; }
    }
}