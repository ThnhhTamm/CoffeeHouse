using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using CoffeeHouseAdmin.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ DỊCH VỤ (SERVICES) ---

builder.Services.AddControllersWithViews();

// Dùng để truy cập Session trong View/Controller
builder.Services.AddHttpContextAccessor();

// Kết nối Database (Chỗ này đã xóa )
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Cấu hình Session (Chỉ cần gọi 1 lần duy nhất này thôi Boss nhé)
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Khách có 30 phút để áp mã và thanh toán
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình Đăng nhập (Google & Facebook)
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddGoogle(options => {
    options.ClientId = "562881061476-m9c7pfel1m7ahmg5jh9duadubifsu6p5.apps.googleusercontent.com";
    options.ClientSecret = "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";
})
.AddFacebook(options => {
    options.AppId = "APP_ID_CUA_BOSS";
    options.AppSecret = "APP_SECRET_CUA_BOSS";
});

// --- 2. XÂY DỰNG APP ---
var app = builder.Build();

// --- 3. CẤU HÌNH PIPELINE (MIDDLEWARE) ---

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// THỨ TỰ CHUẨN: Session phải nằm TRƯỚC Authentication/Authorization
// để hệ thống kịp nhận diện giỏ hàng và khuyến mãi của khách trước khi bắt đăng nhập.
app.UseSession(); 
app.UseAuthentication(); 
app.UseAuthorization();

// --- 4. CẤU HÌNH ĐƯỜNG DẪN (ROUTES) ---

// Route cho khu vực Admin
app.MapControllerRoute(
    name: "MyAreas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Route mặc định cho Khách
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();