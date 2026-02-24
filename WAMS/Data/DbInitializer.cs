using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using WAMS.Models;

namespace WAMS.Data
{
	public static class DbInitializer
	{
		public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
		{
			var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
			var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

			string[] roles = { "Admin", "Employee", "Manager", "HR" };

			// 1. Creating Roles
			foreach (var role in roles)
			{
				if (!await roleManager.RoleExistsAsync(role))
					await roleManager.CreateAsync(new IdentityRole(role));
			}

			// 2. Users (Identity-compliant)
			var admin = await SeedUserAsync(userManager, "admin@workflow.local", "Admin@123", "System Administrator", "Admin");
			var employee = await SeedUserAsync(userManager, "employee@workflow.local", "Employee@123", "Default Employee", "Employee");
			var manager = await SeedUserAsync(userManager, "manager@workflow.local", "Manager@123", "Default Manager", "Manager");
			var hr = await SeedUserAsync(userManager, "hr@workflow.local", "HRAdmin@123", "HR Administrator", "HR");

			// 3. Additional dummy users for testing
			var emily = await SeedUserAsync(userManager, "emily.davis@example.com", "Password123!", "Emily Davis", "Manager");
			var frank = await SeedUserAsync(userManager, "frank.miller@example.com", "Password123!", "Frank Miller", "Manager");
			var alice = await SeedUserAsync(userManager, "alice.williams@example.com", "Password123!", "Alice Williams", "Employee", emily.Id);
			var bob = await SeedUserAsync(userManager, "bob.smith@example.com", "Password123!", "Bob Smith", "Employee", emily.Id);
			var carol = await SeedUserAsync(userManager, "carol.jones@example.com", "Password123!", "Carol Jones", "Employee", frank.Id);
			var david = await SeedUserAsync(userManager, "david.brown@example.com", "Password123!", "David Brown", "Employee", frank.Id);

			// 4. Leave Requests
			if (!context.LeaveRequests.Any())
			{
				context.LeaveRequests.AddRange(
					new EmployeeRequest
					{
						EmployeeId = alice.Id,
						StartDate = DateTime.UtcNow.AddDays(5),
						EndDate = DateTime.UtcNow.AddDays(10),
						Reason = "Family vacation",
						Status = LeaveStatus.Submitted
					},
					new EmployeeRequest
					{
						EmployeeId = bob.Id,
						StartDate = DateTime.UtcNow.AddDays(2),
						EndDate = DateTime.UtcNow.AddDays(4),
						Reason = "Medical appointment",
						Status = LeaveStatus.Draft
					},
					new EmployeeRequest
					{
						EmployeeId = carol.Id,
						StartDate = DateTime.UtcNow.AddDays(7),
						EndDate = DateTime.UtcNow.AddDays(9),
						Reason = "Conference attendance", 
						Status = LeaveStatus.ManagerApproved
					},
					new EmployeeRequest
					{
						EmployeeId = david.Id,
						StartDate = DateTime.UtcNow.AddDays(12),
						EndDate = DateTime.UtcNow.AddDays(14),
						Reason = "Personal errands",
						Status = LeaveStatus.ManagerRejected
					}
				);
				await context.SaveChangesAsync();
			}

			// 5. Approval Actions
			if (!context.ApprovalActions.Any())
			{
				var leaveRequests = context.LeaveRequests.ToList();

				foreach (var leave in leaveRequests)
				{
					if (leave.Status == LeaveStatus.Submitted || leave.Status == LeaveStatus.ManagerApproved || leave.Status == LeaveStatus.ManagerRejected)
					{
						var approverId = context.Users.FirstOrDefault(u => u.Id == leave.Employee.ManagerId)?.Id;
						if (approverId != null)
						{
							context.ApprovalActions.Add(new ApprovalAction
							{
								LeaveRequestId = leave.Id,
								ApproverId = approverId,
								Decision = leave.Status == LeaveStatus.ManagerRejected ? ApprovalDecision.Rejected : ApprovalDecision.Approved,
								Comment = leave.Status == LeaveStatus.ManagerRejected ? "Rejected due to conflict" : "Approved",
								ActionedAt = DateTime.UtcNow
							});
						}
					}
				}
				await context.SaveChangesAsync();
			}

		}

		private static async Task<User> SeedUserAsync(
			UserManager<User> userManager,
			string email,
			string password,
			string fullName,
			string role,
			string? managerId = null)
		{
			var user = await userManager.FindByEmailAsync(email);

			if (user == null)
			{
				user = new User
				{
					UserName = email,
					Email = email,
					FullName = fullName,
					ManagerId = managerId,
					EmailConfirmed = true
				};

				var result = await userManager.CreateAsync(user, password);
				if (!result.Succeeded)
				{
					var errors = string.Join(", ", result.Errors.Select(e => e.Description));
					throw new Exception($"Failed to create user {email}: {errors}");
				}

				await userManager.AddToRoleAsync(user, role);
			}

			return user;
		}
	}
}
