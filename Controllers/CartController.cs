using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CoffeeHouseAdmin.Models;
using Newtonsoft.Json;


namespace CoffeeHouseAdmin.Controllers
{
    public class CartController : Controller
    {
        private readonly string _connectionString = @"Server=.;Database=CoffeeHouseDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
// Thêm hàm này vào để "nuôi" cái GetConn() ở dưới
private string GetConn()
{
    return _connectionString;
}
        // 1. TRANG GIỎ HÀNG
        public IActionResult Index()
        {
            ViewBag.TableId = HttpContext.Session.GetString("SittingTable");
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = new List<CartItem>();
            if (!string.IsNullOrEmpty(cartJson))
            {
                cart = JsonConvert.DeserializeObject<List<CartItem>>(cartJson);
            }
            return View(cart);
        }

        // 2. THÊM MÓN VÀO GIỎ
        [HttpPost]
        public IActionResult AddToCart(int id, string name, decimal price)
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            List<CartItem> cart = string.IsNullOrEmpty(cartJson) 
                ? new List<CartItem>() 
                : JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

            var item = cart.FirstOrDefault(c => c.Id == id);
            if (item == null)
            {
                cart.Add(new CartItem { Id = id, Name = name, Price = price, Quantity = 1 });
            }
            else
            {
                item.Quantity++;
            }

            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
            return RedirectToAction("Index");
        }

 public IActionResult Checkout(string promoCode, decimal? discountAmount)
{
    // 1. Lấy thông tin từ Session (Giữ nguyên của Boss)
    var userId = HttpContext.Session.GetInt32("UserId"); 
    var userName = HttpContext.Session.GetString("UserName");
    var tableId = HttpContext.Session.GetString("SittingTable");

    // 2. Kiểm tra quyền truy cập (Giữ nguyên của Boss)
    if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(tableId))
    {
        TempData["Error"] = "Bạn ơi, vui lòng quét mã tại bàn hoặc đăng nhập nhé! 🥰";
        return RedirectToAction("Login", "Account"); 
    }

    // 3. Lấy giỏ hàng (Giữ nguyên của Boss)
    var cartJson = HttpContext.Session.GetString("Cart");
    if (string.IsNullOrEmpty(cartJson)) return RedirectToAction("Index", "Home");
    var cart = JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

    // 4. LOGIC LẤY ĐỊA CHỈ SHOPEE STYLE (Giữ nguyên của Boss)
    if (userId != null)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            string sql = "SELECT TOP 1 * FROM CustomerAddresses WHERE CustomerId = @uid ORDER BY IsDefault DESC";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            
            conn.Open();
            SqlDataReader rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                ViewBag.DefaultAddress = new CustomerAddress {
                    AddressId = (int)rdr["AddressId"],
                    ReceiverName = rdr["ReceiverName"].ToString(),
                    PhoneNumber = rdr["PhoneNumber"].ToString(),
                    AddressDetail = rdr["AddressDetail"].ToString()
                };
            }
        }
    }

    // --- BƯỚC 6: LẤY VOUCHER TỪ TÚI THẦN KỲ (ĐÃ SỬA ĐỂ HỨNG ĐƯỢC TỪ URL) ---
    decimal discount = 0;

    // Ưu tiên 1: Nếu trên thanh URL có truyền discountAmount, lấy xài luôn!
    if (discountAmount.HasValue)
    {
        discount = discountAmount.Value;
    }
    // Ưu tiên 2: Nếu URL không có (khách vào trực tiếp), lôi từ Session ra làm dự phòng
    else
    {
        var discountStr = HttpContext.Session.GetString("DiscountAmount");
        if (!string.IsNullOrEmpty(discountStr))
        {
            // Thêm đầy đủ System.Globalization để tránh lỗi thiếu thư viện
            decimal.TryParse(discountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out discount);
        }
    }

    // Xử lý lấy mã Code tương tự
    string appliedCode = !string.IsNullOrEmpty(promoCode) 
        ? promoCode 
        : (HttpContext.Session.GetString("AppliedVoucherCode") ?? "");

    // Đẩy thông tin sang View (Giữ nguyên luồng của Boss)
    ViewBag.DiscountAmount = discount;
    ViewBag.AppliedPromoCode = appliedCode;
    // ---------------------------------------------------------------------

    // 5. Gửi thông tin bổ sung sang View (Giữ nguyên của Boss)
    ViewBag.TableId = tableId; 
    ViewBag.UserName = userName;

    return View(cart);
}

[HttpGet]
public async Task<IActionResult> CheckVoucher(string code, decimal orderTotal)
{
    if (string.IsNullOrEmpty(code)) return Json(new { success = false, message = "Vui lòng nhập mã!" });

    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        string sql = @"SELECT * FROM Promotions WHERE PromoCode = @code AND IsActive = 1 AND ExpiryDate >= GETDATE()";
        await conn.OpenAsync();
        using (SqlCommand cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@code", code.Trim());
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    decimal minOrder = Convert.ToDecimal(reader["MinOrderAmount"]);
                    if (orderTotal < minOrder)
                    {
                        return Json(new { success = false, message = $"Mã này chỉ áp dụng cho đơn hàng từ {minOrder:N0}đ trở lên!" });
                    }

                    int percent = Convert.ToInt32(reader["DiscountPercent"]);
                    decimal flatAmount = Convert.ToDecimal(reader["DiscountAmount"]);
                    
                    // Tính số tiền được giảm
                    decimal discount = percent > 0 ? (orderTotal * percent / 100) : flatAmount;
                  // --- THÊM 2 DÒNG NÀY VÀO ĐÂY ---
// --- SỬA ĐOẠN LƯU SESSION Ở ĐÂY ---
        // Lưu dạng InvariantCulture để chắc chắn số không bị lỗi dấu phẩy/chấm
        HttpContext.Session.SetString("DiscountAmount", discount.ToString(CultureInfo.InvariantCulture));
        HttpContext.Session.SetString("AppliedVoucherCode", code.Trim());

                    return Json(new { success = true, discount = discount, message = "Áp dụng mã giảm giá thành công!" });
                }
            }
        }
    }
    return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn!" });
}

// ... (Hàm CheckVoucher cũ của Boss nằm ở đây) ...

// ==========================================================
// 1. LẤY DANH SÁCH VOUCHER KHẢ DỤNG (THÊM MỚI VÀO ĐÂY NÈ BOSS)
// ==========================================================
[HttpGet]
public async Task<IActionResult> GetAvailableVouchers()
{
    List<Promotion> vouchers = new List<Promotion>();
    try 
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            // Lấy tất cả mã đang kích hoạt và còn hạn sử dụng
            string sql = @"SELECT * FROM Promotions 
                           WHERE IsActive = 1 AND ExpiryDate >= GETDATE() 
                           ORDER BY MinOrderAmount ASC";
            
            await conn.OpenAsync();
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        vouchers.Add(new Promotion {
                            PromoId = (int)reader["PromoId"],
                            PromoCode = reader["PromoCode"].ToString(),
                            DiscountPercent = (int)reader["DiscountPercent"],
                            DiscountAmount = (decimal)reader["DiscountAmount"],
                            MinOrderAmount = (decimal)reader["MinOrderAmount"],
                            ExpiryDate = (DateTime)reader["ExpiryDate"]
                        });
                    }
                }
            }
        }
        return Json(vouchers); // Trả về danh sách để Javascript ở View vẽ thành thẻ Shopee
    }
    catch (Exception ex)
    {
        return BadRequest("Lỗi bốc dữ liệu Voucher: " + ex.Message);
    }
}

// ... (Hàm ProcessCheckout tiếp nối ở dưới) ...

[HttpPost]
public async Task<IActionResult> ProcessCheckout(string fullName, string phone, string address, string paymentMethod, string promoCode, decimal discountAmount)
{
    try 
    {
        // 1. LẤY GIỎ HÀNG TỪ SESSION
        var cartJson = HttpContext.Session.GetString("Cart");
        if (string.IsNullOrEmpty(cartJson)) return RedirectToAction("Index", "Home");
        var cart = JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

        // 2. CHUẨN BỊ THÔNG TIN ĐƠN HÀNG
        string tableId = HttpContext.Session.GetString("SittingTable"); 
        string userName = HttpContext.Session.GetString("UserName") ?? "Khách vãng lai";
        
        // GIỮ NGUYÊN LOGIC CŨ - Chỉ trừ thêm số tiền giảm giá discountAmount vào tổng tiền
        decimal subTotal = cart.Sum(i => i.Price * i.Quantity);
        decimal totalAmount = subTotal - discountAmount;
        if (totalAmount < 0) totalAmount = 0;
        
        string orderId = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss");

        // 3. LƯU VÀO DATABASE
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            
            // --- A. LƯU BẢNG ORDERS (Giữ nguyên các cột cũ, chỉ chèn thêm AppliedPromoCode và DiscountAmount ở cuối) ---
            string sqlOrder = @"INSERT INTO Orders (OrderID, CustomerName, FullName, Phone, ShippingAddress, TotalAmount, Status, OrderDate, CreatedAt, PaymentMethod, TableID, AppliedPromoCode, DiscountAmount) 
                                VALUES (@id, @cname, @fname, @phone, @addr, @total, N'Chờ thanh toán', GETDATE(), GETDATE(), @pm, @tableId, @promo, @discount)";
            
            SqlCommand cmdOrder = new SqlCommand(sqlOrder, conn);
            cmdOrder.Parameters.AddWithValue("@id", orderId);
            cmdOrder.Parameters.AddWithValue("@cname", userName);
            cmdOrder.Parameters.AddWithValue("@fname", fullName ?? (object)DBNull.Value);
            cmdOrder.Parameters.AddWithValue("@phone", phone ?? (object)DBNull.Value);
            cmdOrder.Parameters.AddWithValue("@addr", address ?? (object)DBNull.Value);
            cmdOrder.Parameters.AddWithValue("@total", totalAmount); // Số tiền thực tế sau giảm giá
            cmdOrder.Parameters.AddWithValue("@pm", paymentMethod);
            cmdOrder.Parameters.AddWithValue("@tableId", (object)tableId ?? DBNull.Value);
            cmdOrder.Parameters.AddWithValue("@promo", string.IsNullOrEmpty(promoCode) ? (object)DBNull.Value : promoCode);
            cmdOrder.Parameters.AddWithValue("@discount", discountAmount);
            await cmdOrder.ExecuteNonQueryAsync();

            // --- B. LƯU CHI TIẾT ORDERITEMS (Giữ nguyên 100%) ---
            foreach (var item in cart) {
                string sqlItem = "INSERT INTO OrderItems (OrderId, ProductName, Quantity, Price) VALUES (@oid, @pname, @qty, @price)";
                SqlCommand cmdItem = new SqlCommand(sqlItem, conn);
                cmdItem.Parameters.AddWithValue("@oid", orderId);
                cmdItem.Parameters.AddWithValue("@pname", item.Name);
                cmdItem.Parameters.AddWithValue("@qty", item.Quantity);
                cmdItem.Parameters.AddWithValue("@price", item.Price);
                await cmdItem.ExecuteNonQueryAsync();
            }

            // --- C. LOGIC CẬP NHẬT DANH SÁCH KHÁCH HÀNG (GIỮ NGUYÊN 100%) ---
            string sqlCustomer = @"
                IF EXISTS (SELECT 1 FROM Customers WHERE Phone = @p)
                BEGIN
                    -- Nếu đã có khách, cập nhật số đơn và tổng tiền
                    UPDATE Customers 
                    SET TotalOrders = ISNULL(TotalOrders, 0) + 1, 
                        TotalSpent = ISNULL(TotalSpent, 0) + @amt 
                    WHERE Phone = @p
                END
                ELSE
                BEGIN
                    -- Nếu là khách mới, tạo mới (Id tự tăng nên không cần chèn Id)
                    INSERT INTO Customers (FullName, Email, Phone, CreatedAt, Role, TotalOrders, TotalSpent, [Rank], Password)
                    VALUES (@name, @email, @p, GETDATE(), 'Customer', 1, @amt, N'Thành viên', '123456')
                END";

            SqlCommand cmdCust = new SqlCommand(sqlCustomer, conn);
            cmdCust.Parameters.AddWithValue("@p", phone ?? "0000000000"); 
            cmdCust.Parameters.AddWithValue("@amt", totalAmount);
            cmdCust.Parameters.AddWithValue("@name", fullName ?? userName);
            cmdCust.Parameters.AddWithValue("@email", (fullName ?? userName).Replace(" ", "") + "@gmail.com");

            await cmdCust.ExecuteNonQueryAsync();
        }

        // 4. XỬ LÝ THANH TOÁN & DỌN DẸP (Giữ nguyên 100%)
       // 4. XỬ LÝ THANH TOÁN & DỌN DẸP
HttpContext.Session.Remove("Cart");
HttpContext.Session.Remove("DiscountAmount"); // THÊM DÒNG NÀY
        HttpContext.Session.Remove("AppliedVoucherCode");

if (paymentMethod == "COD") 
{
    // Kiểm tra xem khách có đang ngồi tại bàn không (tableId lấy từ Session ở bước 2)
    if (string.IsNullOrEmpty(tableId)) 
    {
        // TRƯỜNG HỢP 1: ĐẶT ONLINE
        TempData["Success"] = "Đặt đơn thành công! Đơn hàng sẽ được giao đến Bạn sớm nhất nhé! 🛵☕";
    }
    else 
    {
        // TRƯỜNG HỢP 2: ĐẶT TẠI BÀN
        TempData["Success"] = $"Đặt đơn thành công! Đồ uống sẽ sớm được mang ra Bàn {tableId} nhé! 🥰";
    }
    
    return RedirectToAction("Index", "Home");
}
else 
{
    // --- PHẦN THANH TOÁN QR GIỮ NGUYÊN ---
    long finalAmount = (long)totalAmount; 
    string info = Uri.EscapeDataString($"THANH TOAN {orderId}");
    string name = Uri.EscapeDataString("NGUYEN THI THANH TAM");
    string qrUrl = $"https://img.vietqr.io/image/MB-0868124023-compact.jpg?amount={finalAmount}&addInfo={info}&accountName={name}";

    ViewBag.QrUrl = qrUrl;
    ViewBag.Type = paymentMethod;
    ViewBag.Amount = totalAmount;
    ViewBag.OrderId = orderId;

    return View("PaymentQR"); 
}
    }
    catch (Exception ex)
    {
        return Content("Lỗi hệ thống rồi Boss ơi: " + ex.Message);
    }
}

[HttpPost]
public IActionResult SyncCart([FromBody] List<CartItem> cart)
{
    if (cart != null)
    {
        // Lưu dữ liệu từ JavaScript gửi lên vào Session để C# dùng được
        HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
        return Ok();
    }
    return BadRequest();
}

// --- 5. ĐỊA CHỈ ĐÓN KHÁCH THANH TOÁN QR (Giữ nguyên 100%) ---
[HttpGet] 
public IActionResult PaymentQR(string type, decimal amount, string orderId)
{
    ViewBag.Type = type;
    ViewBag.Amount = amount;
    ViewBag.OrderId = orderId;
    
    return View(); 
}

// ==========================================
// 6. QUẢN LÝ DANH SÁCH ĐỊA CHỈ (Giữ nguyên 100%)
// ==========================================
[HttpGet]
public IActionResult ManageAddresses()
{
    var userId = HttpContext.Session.GetInt32("UserId"); 
    if (userId == null) return RedirectToAction("Login", "Account");

    List<CustomerAddress> addresses = new List<CustomerAddress>();
    using (SqlConnection conn = new SqlConnection(_connectionString))
    {
        string sql = "SELECT * FROM CustomerAddresses WHERE CustomerId = @uid ORDER BY IsDefault DESC, AddressId DESC";
        SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        conn.Open();
        SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            addresses.Add(new CustomerAddress {
                AddressId = (int)rdr["AddressId"],
                CustomerId = (int)rdr["CustomerId"],
                ReceiverName = rdr["ReceiverName"].ToString(),
                PhoneNumber = rdr["PhoneNumber"].ToString(),
                AddressDetail = rdr["AddressDetail"].ToString(),
                IsDefault = rdr["IsDefault"] != DBNull.Value && (bool)rdr["IsDefault"]
            });
        }
    }
    return View(addresses);
}
        [HttpPost]
        public IActionResult SetDefaultAddress(int addressId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false });

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sqlReset = "UPDATE CustomerAddresses SET IsDefault = 0 WHERE CustomerId = @uid";
                SqlCommand cmdReset = new SqlCommand(sqlReset, conn);
                cmdReset.Parameters.AddWithValue("@uid", userId);
                cmdReset.ExecuteNonQuery();

                string sqlSet = "UPDATE CustomerAddresses SET IsDefault = 1 WHERE AddressId = @aid AND CustomerId = @uid";
                SqlCommand cmdSet = new SqlCommand(sqlSet, conn);
                cmdSet.Parameters.AddWithValue("@aid", addressId);
                cmdSet.Parameters.AddWithValue("@uid", userId);
                cmdSet.ExecuteNonQuery();
            }
            return Json(new { success = true });
        }
       [HttpPost]
public IActionResult AddAddress(string receiverName, string phoneNumber, string addressDetail)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    
    // Nếu bị lỗi này, Boss sẽ thấy thông báo hiện ra ngay trên màn hình
    if (userId == null) 
    {
        return Json(new { success = false, message = "Lỗi: Bé Phin không tìm thấy UserId trong Session! Boss hãy thử Đăng xuất rồi Đăng nhập lại nhé. 🥰" });
    }
    
    // ... (Phần code SQL giữ nguyên nhưng nhớ cho vào khối try-catch) ...
    try {
        using (SqlConnection conn = new SqlConnection(_connectionString)) {
            conn.Open();
            string sql = @"INSERT INTO CustomerAddresses (CustomerId, ReceiverName, PhoneNumber, AddressDetail, IsDefault) 
                           VALUES (@uid, @name, @phone, @addr, 0)";
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@name", receiverName ?? "");
            cmd.Parameters.AddWithValue("@phone", phoneNumber ?? "");
            cmd.Parameters.AddWithValue("@addr", addressDetail ?? "");
            cmd.ExecuteNonQuery();
        }
        return Json(new { success = true });
    }
    catch (Exception ex) {
        return Json(new { success = false, message = "Lỗi Database rồi Boss: " + ex.Message });
    }
}
// Dán hàm này vào CartController.cs
// DÁN ĐÈ HÀM NÀY THAY THẾ CHO HÀM GETORDERITEMS CŨ Ở CUỐI FILE CARTCONTROLLER.CS
[HttpGet] 
public async Task<IActionResult> GetOrderItems(string orderId) 
{
    var items = new List<object>();
    try {
        using (SqlConnection conn = new SqlConnection(_connectionString)) {
            // 🌟 CHỐT HẠ: Chỉ check r.OrderId = @oid. Đơn nào có mã đơn đó thì mới hiện Đã đánh giá!
            string sql = @"SELECT d.ProductName, p.ImageUrl, d.Price, p.Id AS ProductId,
                                  CASE WHEN EXISTS (
                                      SELECT 1 FROM ProductReviews r
                                      WHERE r.ProductId = p.Id AND r.OrderId = @oid
                                  ) THEN 1 ELSE 0 END AS IsReviewed
                           FROM OrderItems d 
                           LEFT JOIN Products p ON d.ProductName = p.Name 
                           WHERE d.OrderId = @oid";
            
            await conn.OpenAsync();
            using (SqlCommand cmd = new SqlCommand(sql, conn)) {
                cmd.Parameters.AddWithValue("@oid", orderId ?? "");
                
                using (var reader = await cmd.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        items.Add(new {
                            productId = reader["ProductId"] != DBNull.Value ? reader["ProductId"] : 0,
                            productName = reader["ProductName"].ToString(),
                            imageUrl = reader["ImageUrl"] != DBNull.Value ? reader["ImageUrl"].ToString() : "https://cdn-icons-png.flaticon.com/512/924/924514.png",
                            price = Convert.ToDecimal(reader["Price"]),
                            isReviewed = Convert.ToInt32(reader["IsReviewed"]) == 1
                        });
                    }
                }
            }
        }
        return Json(items); 
    } catch (Exception ex) {
        return Json(new { success = false, error = ex.Message });
    }
}
    } // Dấu ngoặc này ĐÓNG CLASS (Cực kỳ quan trọng)
    
} // Dấu ngoặc này ĐÓNG NAMESPACE

