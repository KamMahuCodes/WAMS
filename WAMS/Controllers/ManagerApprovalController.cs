using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WAMS.Data;
using WAMS.Models;

namespace WorkflowSystem.Controllers
{
	[Authorize(Roles = "Manager")]
	public class ManagerApprovalController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<User> _userManager;

		public ManagerApprovalController(
			ApplicationDbContext context,
			UserManager<User> userManager)
		{
			_context = context;
			_userManager = userManager;
			
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

			if (request == null)
				return NotFound();

			if (request.Status != LeaveStatus.Submitted)
				return BadRequest("This request is not awaiting approval.");

			if (request.Employee.ManagerId != managerId)
				return Forbid();

			if (request.ApprovalActions.Any(a => a.ApproverId == managerId))
				return BadRequest("You have already processed this request.");

			// Get HR user
			var hrUsers = await _userManager.GetUsersInRoleAsync("HR");
			var hr = hrUsers.FirstOrDefault();

			if (hr == null)
				return BadRequest("No HR user found.");

			request.Status = LeaveStatus.ManagerApproved;

			_context.ApprovalActions.Add(new ApprovalAction
			{
				LeaveRequestId = request.Id,
				ApproverId = managerId,
				Decision = ApprovalDecision.Approved,
				Comment = comments,
				ActionedAt = DateTime.UtcNow
			});

			
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// ============================
		// POST: /ManagerApproval/Reject/5
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Reject(int id, string comments)
		{
			var managerId = GetUserId();

			var request = await _context.LeaveRequests
				.Include(l => l.Employee)
				.Include(l => l.ApprovalActions)
				.FirstOrDefaultAsync(l => l.Id == id);

			if (request == null)
				return NotFound();

			if (request.Status != LeaveStatus.Submitted)
				return BadRequest("This request is not awaiting approval.");

			if (request.Employee.ManagerId != managerId)
				return Forbid();

			if (request.ApprovalActions.Any(a => a.ApproverId == managerId))
				return BadRequest("You have already processed this request.");

			request.Status = LeaveStatus.ManagerRejected;

			_context.ApprovalActions.Add(new ApprovalAction
			{
				LeaveRequestId = request.Id,
				ApproverId = managerId,
				Decision = ApprovalDecision.Rejected,
				Comment = comments,
				ActionedAt = DateTime.UtcNow
			});

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}
	}
}
