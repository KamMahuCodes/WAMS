using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WAMS.Data;
using WAMS.Models;
using WAMS.Services;

namespace WAMS.Controllers
{
	[Authorize(Roles = "Employee")]
	public class LeaveRequestController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<User> _userManager;
		private readonly IEmailService _emailService;

		public LeaveRequestController(
			ApplicationDbContext context,
			UserManager<User> userManager,
			IEmailService emailService)
		{
			_context = context;
			_userManager = userManager;
			_emailService = emailService;
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

			// Get manager (FIXED)
			if (string.IsNullOrEmpty(employee.ManagerId))
				return BadRequest("Manager not assigned.");

			var manager = await _userManager.Users
				.FirstOrDefaultAsync(u => u.Id == employee.ManagerId);

			if (manager == null)
				return BadRequest("Manager not found.");

			// Update status
			request.Status = LeaveStatus.Submitted;

			// Notify manager
			await _emailService.SendAsync(
				manager.Email,
				"New Leave Request Submitted",
				$@"
                    <p>Dear {manager.FullName},</p>
                    <p>{employee.FullName} has submitted a leave request.</p>
                    <p><strong>Dates:</strong> {request.StartDate:d} - {request.EndDate:d}</p>
                    <p>Please log in to review and approve.</p>"
			);

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// ============================
		// GET: LeaveRequest/Details/5
		// ============================
		//public async Task<IActionResult> Details(int id)
		//{
		//	var userId = GetUserId();

		//	var request = await _context.LeaveRequests
		//		.Include(l => l.ApprovalActions)
		//			.ThenInclude(a => a.Approver)
		//		.FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == userId);

		//	if (request == null)
		//		return NotFound();

		//	return View(request);
		//}
	}
}
