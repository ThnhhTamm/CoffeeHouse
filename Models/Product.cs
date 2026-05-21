using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // 🌟 ĐÂY NÈ BOSS, DÁN DÒNG NÀY LÊN ĐẦU FILE NHÉ!

namespace CoffeeHouseAdmin.Models // Sửa lỗi 'amespace' ở đây nè Boss!
{
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        
        // Để decimal cho chuẩn với giá 45.000,00 dưới DB
        public decimal Price { get; set; } 
        
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }

        // Đã sửa thành string vì DB chứa "1/5"
        public string? BitternessLevel { get; set; } 

        public int? Stock { get; set; } // Nếu DB báo lỗi Int32 ở đây, Boss hãy kiểm tra kiểu dữ liệu trong SQL nhé!
        
        public string? RoastLevel { get; set; }
        public string? FlavorProfile { get; set; }
        public string? Description { get; set; }

        // Sửa List<string> thành string? để khớp với 1 cột trong Database
        public string? Toppings { get; set; } 
        // 🌟 SỬA TẠI ĐÂY: Thêm thẻ [NotMapped] ngay phía trên 2 thuộc tính mới
        [NotMapped]
        public double AvgRating { get; set; } = 0;

        [NotMapped]
        public int ReviewCount { get; set; } = 0;
    }
}