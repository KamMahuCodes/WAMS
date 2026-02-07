using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAMS.Data;
using WAMS.Models;
using WAMS.Models.ViewModels;

namespace WAMS.Controllers
{
	[Authorize(Roles = "Admin")]
	public class AdminController : Controller
	{
		private readonly ApplicationDbContext _context;

		public AdminController(ApplicationDbContext context)
		{
			_context = context;
		}

		
		// ============================
		// Users
		// GET: /Admin/Users
		// ============================
		public async Task<IActionResult> Users()
		{
			// Fetch all users
			var users = await _context.Users
				.Include(u => u.Manager)
				.Select(u => new UserList
				{
					Id = u.Id,
					FullName = u.FullName,
					Email = u.Email,
					ManagerName = u.Manager != null ? u.Manager.FullName : null
				})
				.ToListAsync();

			return View(users);
		}

		// ============================
		// Leave Requests
		// GET: /Admin/LeaveRequest
		// ============================
		public async Task<IActionResult> LeaveRequest()
		{
			// Fetch all leave requests including employee info
			var requests = await _context.LeaveRequests
				.Include(l => l.Employee)
				.OrderByDescending(l => l.CreatedAt)
				.ToListAsync();

			return View(requests); // Pass requests to the view
		}

		// ============================
		// Reports
		// GET: /Admin/Reports
		// ============================
		public async Task<IActionResult> Reports()
		{
			var reportData = await _context.LeaveRequests
				.GroupBy(l => l.Status)
				.Select(g => new LeaveStatusReport
				{
					Status = g.Key,
					Count = g.Count()
				})
				.ToListAsync();

			return View(reportData);
		}

		// ============================
		// Settings
		// GET: /Admin/Settings
		// ============================
		//public async Task<IActionResult> Settings()
		//{
		//	// Example: Fetch all roles or configurations
		//	var roles = await _context.Roles.ToListAsync(); // assuming you have a Roles table
		//	return View(roles); // Pass roles to the view
		//}
	}
}
