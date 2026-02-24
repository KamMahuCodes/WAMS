using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using WAMS.Hubs;
using WAMS.Data;
using WAMS.Models;

namespace WAMS.Controllers
{
	[Authorize(Roles = "Employee")]
	public class LeaveRequestController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<User> _userManager;
		
		private readonly IHubContext<NotificationHub> _hubContext;

		public LeaveRequestController(
			ApplicationDbContext context,
			UserManager<User> userManager,
			
			IHubContext<NotificationHub> hubContext)
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
		// GET: LeaveRequest
		// ============================
		public async Task<IActionResult> Index()
		{
			var userId = GetUserId();

			var requests = await _context.LeaveRequests
				.Include(l => l.ApprovalActions)
					.ThenInclude(a => a.Approver)
				.Where(l => l.EmployeeId == userId)
				.OrderByDescending(l => l.CreatedAt)
				.ToListAsync();

			return View(requests);
		}

		// ============================
		// GET: LeaveRequest/Create
		// ============================
		public IActionResult Create()
		{
			return View();
		}

		// ============================
		// POST: LeaveRequest/Create
		// Save as Draft
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(EmployeeRequest model)
		{
			if (!ModelState.IsValid)
				return View(model);

			model.EmployeeId = GetUserId();
			model.Status = LeaveStatus.Draft;
			model.CreatedAt = DateTime.UtcNow;

			_context.LeaveRequests.Add(model);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index));
		}

		// ============================
		// GET: LeaveRequest/Edit/5
		// Drafts Only
		// ============================
		public async Task<IActionResult> Edit(int id)
		{
			var userId = GetUserId();

			var request = await _context.LeaveRequests
				.FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == userId);

			if (request == null)
				return NotFound();

			if (request.Status != LeaveStatus.Draft)
				return BadRequest("Only draft requests can be edited.");

			return View(request);
		}

		// ============================
		// POST: LeaveRequest/Edit/5
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, EmployeeRequest model)
		{
			var userId = GetUserId();

			var existing = await _context.LeaveRequests
				.FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == userId);

			if (existing == null)
				return NotFound();

			if (existing.Status != LeaveStatus.Draft)
				return BadRequest("Only draft requests can be edited.");

			existing.StartDate = model.StartDate;
			existing.EndDate = model.EndDate;
			existing.Reason = model.Reason;

			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index));
		}

		// ============================
		// POST: LeaveRequest/Submit/5
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Submit(int id)
		{
			var userId = GetUserId();

			var request = await _context.LeaveRequests
				.FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == userId);

			if (request == null)
				return NotFound();

			if (request.Status != LeaveStatus.Draft)
				return BadRequest("Only draft requests can be submitted.");

			// Get employee
			var employee = await _userManager.Users
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (employee == null)
				return NotFound("Employee not found.");

			// Get manager
			User? manager = null;

			if (!string.IsNullOrEmpty(employee.ManagerId))
			{
				manager = await _userManager.Users
					.FirstOrDefaultAsync(u => u.Id == employee.ManagerId);
			}
			else
			{
				// fallback to default manager
				manager = await _userManager.Users
					.FirstOrDefaultAsync(u => u.Email == "manager@workflow.local");
			}

			
			if (manager == null)
				return BadRequest("Manager not found.");

			// Update status
			request.Status = LeaveStatus.Submitted;

			// Create DB notification
			var notification = new Notification
			{
				UserId = manager.Id,
				Message = $"{employee.FullName} submitted a leave request from {request.StartDate:d} to {request.EndDate:d}"
			};

			_context.Notifications.Add(notification);
			await _context.SaveChangesAsync();

			// Push real-time notification
			await _hubContext.Clients.User(manager.Id)
				.SendAsync("ReceiveNotification", notification.Message);

			
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		 //============================
		// GET: LeaveRequest/Details/5
		 //============================
		public async Task<IActionResult> Details(int id)
		{
			var userId = GetUserId();

			var request = await _context.LeaveRequests
				.Include(l => l.ApprovalActions)
					.ThenInclude(a => a.Approver)
				.FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == userId);

			if (request == null)
				return NotFound();

			return View(request);
		}
	}
}
