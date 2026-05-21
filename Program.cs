using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using CoffeeHouseAdmin.Data;


var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ DỊCH VỤ (SERVICES) ---

// --- 1. ĐĂNG KÝ DỊCH VỤ (SERVICES) ---

builder.Services.AddControllersWithViews();

// Dùng để truy cập Session trong View/Controller
builder.Services.AddHttpContextAccessor();

// Kết nối Database PostgreSQL trên Render (Đã đóng ngoặc }); đầy đủ)
// Kết nối Database PostgreSQL nội bộ trên Render
// Kết nối Database PostgreSQL nội bộ có SSL trên Render
builder.Services.AddDbContext<ApplicationDbContext>(options => {
    options.UseNpgsql("Server=dpg-d87m9p67r5hc738ph9u0-a.singapore-postgres.render.com;Database=coffeehousedb;Port=5432;User Id=coffeehousedb_user;Password=g5EGgOlb4B0ro32QE8ZTS9rFilgcUBKM;SslMode=Require;Trust Server Certificate=true;");
});

// Cấu hình Session
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình Đăng nhập (Google & Facebook)

// Cấu hình Đăng nhập (Google & Facebook) - ĐÃ VÁ ĐỦ HÀM ADDAUTHENTICATION
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
// --- TỰ ĐỘNG KHỞI TẠO BẢNG DATABASE NẾU CHƯA CÓ ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // Lệnh thần thánh tự tạo cấu trúc bảng sang Postgres
    }
    catch (Exception ex)
    {
        Console.WriteLine("Lỗi khởi tạo DB: " + ex.Message);
    }
}

// --- 3. CẤU HÌNH PIPELINE (MIDDLEWARE) ---

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
     app.UseHsts();
 }
// Chèn dòng thần thánh này vào để ép web hiện chi tiết lỗi đỏ lòm
app.UseDeveloperExceptionPage();

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