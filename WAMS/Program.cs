using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WAMS.Data;
using WAMS.Models;
using WAMS.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();

builder.Services.Configure<EmailSettings>(
	builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var app = builder.Build();

// Seed roles & users
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	DbInitializer.SeedRolesAndAdminAsync(services).Wait();
}

// Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();          // ✅ REQUIRED

app.UseAuthentication();   // ✅ REQUIRED
app.UseAuthorization();    // ✅ REQUIRED

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
