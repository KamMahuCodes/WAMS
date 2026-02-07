using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WAMS.Data;
using WAMS.Models;
using WAMS.Models.ViewModels;

namespace WAMS.Controllers
{
	[Authorize(Roles = "Admin,HR,Manager,Employee")]
	public class DashboardController : Controller
	{
		private readonly ApplicationDbContext _context;

		public DashboardController(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			var roles = User.Claims
				.Where(c => c.Type == ClaimTypes.Role)
				.Select(c => c.Value)
				.ToList();

			if (User.IsInRole("Admin"))
				return View("Admin", await BuildAdminDashboard());

			if (User.IsInRole("HR"))
				return View("HR", await BuildHRDashboard());

			if (User.IsInRole("Manager"))
				return View("Manager", await BuildManagerDashboard());

			return View("Employee", await BuildEmployeeDashboard());
		}

		public async Task<Dashboard> BuildAdminDashboard()
		{
			var model = new Dashboard();

			// ========================
			// COUNTS
			// ========================

			model.PendingApprovals = await _context.LeaveRequests
				.CountAsync(l =>
					l.Status == LeaveStatus.Submitted ||
					l.Status == LeaveStatus.ManagerApproved);

			model.ApprovedRequests = await _context.LeaveRequests
				.CountAsync(l => l.Status == LeaveStatus.HRApproved);

			model.RejectedRequests = await _context.LeaveRequests
				.CountAsync(l =>
					l.Status == LeaveStatus.ManagerRejected ||
					l.Status == LeaveStatus.HRRejected);

			model.Users = await _context.Users.CountAsync();

			// ========================
			// REQUESTS PER MONTH
			// ========================

			var monthlyData = await _context.LeaveRequests
				.Where(l => l.CreatedAt != null)
				.GroupBy(l => new
				{
					Year = l.CreatedAt.Year,
					Month = l.CreatedAt.Month
				})
				.OrderBy(g => g.Key.Year)
				.ThenBy(g => g.Key.Month)
				.Select(g => new
				{
					Month = $"{g.Key.Year}-{g.Key.Month:D2}",
					Count = g.Count()
				})
				.ToListAsync();

			model.Months = monthlyData.Select(m => m.Month).ToList();
			model.RequestsPerMonth = monthlyData.Select(m => m.Count).ToList();

			model.RecentRequests = await _context.LeaveRequests
			.Include(l => l.Employee)
			.Include(l => l.Manager)
			.OrderByDescending(l => l.CreatedAt)
			.Take(5)
			.Select(l => new RecentLeaveRequest
			{
				EmployeeName = l.Employee.FullName,
				ManagerName = l.Manager.FullName,
				Status = l.Status,
				CreatedAt = l.CreatedAt,
				Reason = l.Reason
			})
			.ToListAsync();

			return model;
		}


		private async Task<HRDashboardViewModel> BuildHRDashboard()
		{
			return new HRDashboardViewModel
			{
				PendingHRApprovals = await _context.LeaveRequests
					.CountAsync(l => l.Status == LeaveStatus.ManagerApproved),

				ApprovedByHR = await _context.LeaveRequests
					.CountAsync(l => l.Status == LeaveStatus.HRApproved),

				RejectedByHR = await _context.LeaveRequests
					.CountAsync(l => l.Status == LeaveStatus.HRRejected)
			};
		}

		private async Task<ManagerDashboardViewModel> BuildManagerDashboard()
		{
			var managerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

			return new ManagerDashboardViewModel
			{
				PendingTeamApprovals = await _context.LeaveRequests
					.CountAsync(l => l.Status == LeaveStatus.Submitted),

				ApprovedByManager = await _context.LeaveRequests
					.CountAsync(l => l.Status == LeaveStatus.ManagerApproved),

				RejectedByManager = await _context.LeaveRequests
					.CountAsync(l => l.Status == LeaveStatus.ManagerRejected)
			};
		}

		private async Task<EmployeeDashboardViewModel> BuildEmployeeDashboard()
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

			return new EmployeeDashboardViewModel
			{
				MyPendingRequests = await _context.LeaveRequests
					.CountAsync(l => l.EmployeeId == userId && l.Status == LeaveStatus.Submitted),

				MyApprovedRequests = await _context.LeaveRequests
					.CountAsync(l => l.EmployeeId == userId && l.Status == LeaveStatus.HRApproved),

				MyRejectedRequests = await _context.LeaveRequests
					.CountAsync(l => l.EmployeeId == userId &&
						(l.Status == LeaveStatus.ManagerRejected ||
						 l.Status == LeaveStatus.HRRejected))
			};
		}

	}
}
