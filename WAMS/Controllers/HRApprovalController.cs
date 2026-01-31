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
	[Authorize(Roles = "HR")]
	public class HRApprovalController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<User> _userManager;
		private readonly IEmailService _emailService;

		public HRApprovalController(
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
		// GET: /HRApproval
		// ============================
		public async Task<IActionResult> Index()
		{
			var requests = await _context.LeaveRequests
				.Include(l => l.Employee)
				.Where(l => l.Status == LeaveStatus.ManagerApproved)
				.OrderBy(l => l.CreatedAt)
				.ToListAsync();

			return View(requests);
		}

		// ============================
		// GET: /HRApproval/Details/5
		// ============================
		public async Task<IActionResult> Details(int id)
		{
			var request = await _context.LeaveRequests
				.Include(l => l.Employee)
				.Include(l => l.ApprovalActions)
					.ThenInclude(a => a.Approver)
				.FirstOrDefaultAsync(l =>
					l.Id == id &&
					l.Status == LeaveStatus.ManagerApproved);

			if (request == null)
				return NotFound();

			return View(request);
		}

		// ============================
		// POST: /HRApproval/Approve/5
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Approve(int id, string comments)
		{
			var hrId = GetUserId();

			var request = await _context.LeaveRequests
				.Include(l => l.Employee)
				.Include(l => l.ApprovalActions)
				.FirstOrDefaultAsync(l => l.Id == id);

			if (request == null)
				return NotFound();

			if (request.Status != LeaveStatus.ManagerApproved)
				return BadRequest("Request is not ready for HR approval.");

			if (request.ApprovalActions.Any(a => a.ApproverId == hrId))
				return BadRequest("You have already processed this request.");

			request.Status = LeaveStatus.HRApproved;

			_context.ApprovalActions.Add(new ApprovalAction
			{
				LeaveRequestId = request.Id,
				ApproverId = hrId,
				Decision = ApprovalDecision.Approved,
				Comment = comments,
				ActionedAt = DateTime.UtcNow
			});

			// Notify employee
			await _emailService.SendAsync(
				request.Employee.Email,
				"Leave Request Approved",
				$@"
                    <p>Dear {request.Employee.FullName},</p>
                    <p>Your leave request has been <strong>approved</strong>.</p>
                    <p><strong>Dates:</strong> {request.StartDate:d} - {request.EndDate:d}</p>
                ");

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// ============================
		// POST: /HRApproval/Reject/5
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Reject(int id, string comments)
		{
			var hrId = GetUserId();

			var request = await _context.LeaveRequests
				.Include(l => l.Employee)
				.Include(l => l.ApprovalActions)
				.FirstOrDefaultAsync(l => l.Id == id);

			if (request == null)
				return NotFound();

			if (request.Status != LeaveStatus.ManagerApproved)
				return BadRequest("Request is not ready for HR approval.");

			if (request.ApprovalActions.Any(a => a.ApproverId == hrId))
				return BadRequest("You have already processed this request.");

			request.Status = LeaveStatus.HRRejected;

			_context.ApprovalActions.Add(new ApprovalAction
			{
				LeaveRequestId = request.Id,
				ApproverId = hrId,
				Decision = ApprovalDecision.Rejected,
				Comment = comments,
				ActionedAt = DateTime.UtcNow
			});

			// Notify employee
			await _emailService.SendAsync(
				request.Employee.Email,
				"Leave Request Rejected",
				$@"
                    <p>Dear {request.Employee.FullName},</p>
                    <p>Your leave request has been <strong>rejected</strong>.</p>
                    <p><strong>Reason:</strong> {comments}</p>
                ");

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}
	}
}
