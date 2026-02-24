using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WAMS.Data;
using WAMS.Models;

namespace WAMS.Controllers
{
	[Authorize(Roles = "HR")]
	public class HRApprovalController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<User> _userManager;
		
		public HRApprovalController(
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
		private async Task LoadManagersToViewBag()
		{
			var managers = await _userManager.GetUsersInRoleAsync("Manager");
			ViewBag.Managers = managers;
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

			
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		// ============================
		// GET: /HRApproval/CreateManager
		// ============================
		public IActionResult CreateManager()
		{
			return View();
		}

		// ============================
		// POST: /HRApproval/CreateManager
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateManager(string fullName, string email, string password)
		{
			if (string.IsNullOrWhiteSpace(fullName) ||
				string.IsNullOrWhiteSpace(email) ||
				string.IsNullOrWhiteSpace(password))
			{
				ModelState.AddModelError("", "All fields are required.");
				return View();
			}

			var user = new User
			{
				FullName = fullName,
				UserName = email,
				Email = email,
				EmailConfirmed = true
			};

			var result = await _userManager.CreateAsync(user, password);

			if (result.Succeeded)
			{
				await _userManager.AddToRoleAsync(user, "Manager");
				return RedirectToAction(nameof(Index));
			}

			foreach (var error in result.Errors)
				ModelState.AddModelError("", error.Description);

			return View();
		}

		// ============================
		// GET: /HRApproval/CreateEmployee
		// ============================
		public async Task<IActionResult> CreateEmployee()
		{
			var managers = await _userManager.GetUsersInRoleAsync("Manager");

			ViewBag.Managers = managers;

			return View();
		}

		// ============================
		// POST: /HRApproval/CreateEmployee
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateEmployee(
			string fullName,
			string email,
			string password,
			string managerId)
		{
			if (string.IsNullOrWhiteSpace(fullName) ||
				string.IsNullOrWhiteSpace(email) ||
				string.IsNullOrWhiteSpace(password) ||
				string.IsNullOrWhiteSpace(managerId))
			{
				ModelState.AddModelError("", "All fields are required.");
				await LoadManagersToViewBag();
				return View();
			}

			var user = new User
			{
				FullName = fullName,
				UserName = email,
				Email = email,
				EmailConfirmed = true,
				ManagerId = managerId 
			};

			var result = await _userManager.CreateAsync(user, password);

			if (result.Succeeded)
			{
				await _userManager.AddToRoleAsync(user, "Employee");
				return RedirectToAction(nameof(Index));
			}

			foreach (var error in result.Errors)
				ModelState.AddModelError("", error.Description);

			await LoadManagersToViewBag();
			return View();
		}
	}
}
