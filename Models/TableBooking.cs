#nullable enable
using System;

using System.ComponentModel.DataAnnotations;

// ... các dòng using khác
public class TableBooking {
    [Key]
    public int Id { get; set; }

    [Required]
    public string? CustomerEmail { get; set; } // Thêm dấu ?

    [Required]
    public string? CustomerPhone { get; set; } // Thêm dấu ?

    public string? CoffeeTableID { get; set; } // Đảm bảo là string?

    public DateTime BookingDate { get; set; }

    [Required]
    public string? BookingTime { get; set; } // Thêm dấu ?

    public int NumberOfPeople { get; set; }

    public string Status { get; set; } = "Chờ xác nhận";
}
