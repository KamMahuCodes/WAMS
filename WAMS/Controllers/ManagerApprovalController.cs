using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WAMS.Data;
using WAMS.Hubs;
using WAMS.Models;

namespace WorkflowSystem.Controllers
{
	[Authorize(Roles = "Manager")]
	public class ManagerApprovalController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<User> _userManager;
		private readonly IHubContext<NotificationHub> _hubContext;
		public ManagerApprovalController(
			ApplicationDbContext context,
			UserManager<User> userManager,IHubContext<NotificationHub> hubContext)
		{
			_context = context;
			_userManager = userManager;
			_hubContext = hubContext;
		}

		private string GetUserId()
		{
			return User.FindFirstValue(ClaimTypes.NameIdentifier);
		}

		// ============================
		// GET: /ManagerApproval/ Approved
		// ============================
		public async Task<IActionResult> Index()
		{
			var managerId = GetUserId();

			var requests = await _context.LeaveRequests
				.Include(l => l.Employee)
				.Where(l =>
					l.Status == LeaveStatus.ManagerApproved && l.Status != LeaveStatus.HRApproved &&
					l.Employee.ManagerId == managerId
					)
				.OrderBy(l => l.CreatedAt)
				.ToListAsync();

			return View(requests);
		}

		//=============================
		//Get: /ManagerApproval/Rejected
		//=============================
		public async Task<IActionResult> Rejected()
		{
			var managerId = GetUserId();

			var requests = await _context.LeaveRequests
				.Include(l => l.Employee)
				.Where(l =>
					l.Status == LeaveStatus.ManagerRejected && 
					l.Employee.ManagerId == managerId
					)
				.OrderBy(l => l.CreatedAt)
				.ToListAsync();

			return View(requests);
		}

		//=============================
		// Get: /ManagerApproval/NewRequests
		//=============================
		public async Task<IActionResult> NewRequests() 
		{
			var managerId = GetUserId();

			var requests = await _context.LeaveRequests
				.Include(l => l.Employee)
				.Where(l =>
					l.Status == LeaveStatus.Submitted && l.Status != LeaveStatus.ManagerApproved &&
					l.Employee.ManagerId == managerId  
					)
				.OrderBy(l => l.CreatedAt)
				.ToListAsync();

			return View(requests);
		}

		// ============================
		// GET: /ManagerApproval/Details/5
		// ============================
		public async Task<IActionResult> Details(int id)
		{
			var managerId = GetUserId();

			var request = await _context.LeaveRequests
				.Include(l => l.Employee)
				.Include(l => l.ApprovalActions)
					.ThenInclude(a => a.Approver)
				.FirstOrDefaultAsync(l =>
					l.Id == id &&
					l.Employee.ManagerId == managerId);

			if (request == null)
				return NotFound();

			return View(request);
		}

		// ============================
		// POST: /ManagerApproval/Approve/5
		// ============================
		

			[HttpPost]
			[ValidateAntiForgeryToken]
			public async Task<IActionResult> Approve(int id, string comments)
			{
				var managerId = GetUserId();

				var request = await _context.LeaveRequests
					.Include(l => l.Employee)
					.Include(l => l.ApprovalActions)
					.FirstOrDefaultAsync(l => l.Id == id);

				if (request == null) return NotFound();
				if (request.Status != LeaveStatus.Submitted) return BadRequest("This request is not awaiting approval.");
				if (request.Employee.ManagerId != managerId) return Forbid();
				if (request.ApprovalActions.Any(a => a.ApproverId == managerId)) return BadRequest("Already processed.");

				request.Status = LeaveStatus.ManagerApproved;

				_context.ApprovalActions.Add(new ApprovalAction
				{
					LeaveRequestId = request.Id,
					ApproverId = managerId,
					Decision = ApprovalDecision.Approved,
					Comment = comments,
					ActionedAt = DateTime.UtcNow
				});

				// Notify HR
				var hrUsers = await _userManager.GetUsersInRoleAsync("HR");
				foreach (var hr in hrUsers)
				{
					var message = $"Manager approved leave request for {request.Employee.FullName} ({request.StartDate:d} - {request.EndDate:d})";
					_context.Notifications.Add(new Notification
					{
						UserId = hr.Id,
						Message = message
					});

					await _hubContext.Clients.User(hr.Id).SendAsync("ReceiveNotification", message);
				}

				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}

			[HttpPost]
			[ValidateAntiForgeryToken]
			public async Task<IActionResult> Reject(int id, string comments)
			{
				var managerId = GetUserId();

				var request = await _context.LeaveRequests
					.Include(l => l.Employee)
					.Include(l => l.ApprovalActions)
					.FirstOrDefaultAsync(l => l.Id == id);

				if (request == null) return NotFound();
				if (request.Status != LeaveStatus.Submitted) return BadRequest("This request is not awaiting approval.");
				if (request.Employee.ManagerId != managerId) return Forbid();
				if (request.ApprovalActions.Any(a => a.ApproverId == managerId)) return BadRequest("Already processed.");

				request.Status = LeaveStatus.ManagerRejected;

				_context.ApprovalActions.Add(new ApprovalAction
				{
					LeaveRequestId = request.Id,
					ApproverId = managerId,
					Decision = ApprovalDecision.Rejected,
					Comment = comments,
					ActionedAt = DateTime.UtcNow
				});

				// Notify HR
				var hrUsers = await _userManager.GetUsersInRoleAsync("HR");
				foreach (var hr in hrUsers)
				{
					var message = $"Manager rejected leave request for {request.Employee.FullName} ({request.StartDate:d} - {request.EndDate:d})";
					_context.Notifications.Add(new Notification
					{
						UserId = hr.Id,
						Message = message
					});

					await _hubContext.Clients.User(hr.Id).SendAsync("ReceiveNotification", message);
				}

				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
		
	}
}
