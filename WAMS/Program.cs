using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WAMS.Data;
using WAMS.Models;
using WAMS.Services;

var builder = WebApplication.CreateBuilder(args);

// =====================
// Services
// =====================

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (NO UI)
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
	options.SignIn.RequireConfirmedAccount = false;
	options.Password.RequireDigit = true;
	options.Password.RequiredLength = 6;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = false;
	options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Authentication cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Login/Login";
	options.AccessDeniedPath = "/Login/Login";
	options.Cookie.HttpOnly = true;
	options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
	options.Cookie.SameSite = SameSiteMode.Lax;
	options.SlidingExpiration = true;
});

// Email services
builder.Services.Configure<EmailSettings>(
	builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var app = builder.Build();

// =====================
// Seed roles & admin user
// =====================

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	await DbInitializer.SeedRolesAndAdminAsync(services);
}

// =====================
// Middleware pipeline
// =====================

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
