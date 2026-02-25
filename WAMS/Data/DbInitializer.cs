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
			var manager = await SeedUserAsync(userManager, "manager@workflow.local", "Manager@123", "Default Manager", "Manager");
			var hr = await SeedUserAsync(userManager, "hr@workflow.local", "HRAdmin@123", "HR Administrator", "HR");

			// 3. Additional dummy users for testing
			var emily = await SeedUserAsync(userManager, "emily.davis@example.com", "Password123!", "Emily Davis", "Manager");
			emily.Department = "IT";
			emily.AnnualLeaveBalance = 18;

			var frank = await SeedUserAsync(userManager, "frank.miller@example.com", "Password123!", "Frank Miller", "Manager");
			frank.Department = "Finance";
			frank.AnnualLeaveBalance = 20;

			var alice = await SeedUserAsync(userManager, "alice.williams@example.com", "Password123!", "Alice Williams", "Employee", emily.Id);
			alice.Department = "IT";
			alice.AnnualLeaveBalance = 15;

			var bob = await SeedUserAsync(userManager, "bob.smith@example.com", "Password123!", "Bob Smith", "Employee", emily.Id);
			bob.Department = "IT";
			bob.AnnualLeaveBalance = 12;

			var carol = await SeedUserAsync(userManager, "carol.jones@example.com", "Password123!", "Carol Jones", "Employee", frank.Id);
			carol.Department = "Finance";
			carol.AnnualLeaveBalance = 17;

			var david = await SeedUserAsync(userManager, "david.brown@example.com", "Password123!", "David Brown", "Employee", frank.Id);
			david.Department = "Finance";
			david.AnnualLeaveBalance = 10;

			await context.SaveChangesAsync();


			var publicHolidays = new List<DateTime>
			{
				new DateTime(DateTime.UtcNow.Year, 1, 1),   // New Year's Day
				new DateTime(DateTime.UtcNow.Year, 3, 21),  // Human Rights Day
				new DateTime(DateTime.UtcNow.Year, 4, 27),  // Freedom Day
				new DateTime(DateTime.UtcNow.Year, 5, 1),   // Workers' Day
				new DateTime(DateTime.UtcNow.Year, 6, 16),  // Youth Day
				new DateTime(DateTime.UtcNow.Year, 9, 24),  // Heritage Day
				new DateTime(DateTime.UtcNow.Year, 12, 25)  // Christmas
			};


			// 4. Leave Requests
			if (!context.LeaveRequests.Any())
			{
				var now = DateTime.UtcNow;
				var rand = new Random();

				var employees = context.Users
					.Where(u => u.Department != null && u.Department != "")
					.ToList();

				for (int i = 0; i < 25; i++)
				{
					var employee = employees[rand.Next(employees.Count)];

					var start = now.AddDays(rand.Next(-90, 90));
					var duration = rand.Next(2, 8);
					var end = start.AddDays(duration);

					var workingDays = CalculateWorkingDays(start, end, publicHolidays);

					if (employee.AnnualLeaveBalance >= workingDays)
					{
						employee.AnnualLeaveBalance -= workingDays;

						var statuses = new[]
						{
							LeaveStatus.Submitted,
							LeaveStatus.ManagerApproved,
							LeaveStatus.ManagerRejected
						};

						context.LeaveRequests.Add(new EmployeeRequest
						{
							EmployeeId = employee.Id,
							StartDate = start,
							EndDate = end,
							Reason = $"Annual Leave - Auto generated request {i + 1}",
							Status = statuses[rand.Next(statuses.Length)]
						});
					}
				}

				await context.SaveChangesAsync();
			}

			// 5. Approval Actions
			if (!context.ApprovalActions.Any())
			{
				var rand = new Random();
				var requests = context.LeaveRequests.ToList();

				foreach (var request in requests)
				{
					if (request.Status == LeaveStatus.ManagerApproved ||
						request.Status == LeaveStatus.ManagerRejected)
					{
						var managerId = context.Users
							.Where(u => u.Id == request.Employee.ManagerId)
							.Select(u => u.Id)
							.FirstOrDefault();

						if (managerId != null)
						{
							context.ApprovalActions.Add(new ApprovalAction
							{
								LeaveRequestId = request.Id,
								ApproverId = managerId,
								Decision = request.Status == LeaveStatus.ManagerRejected
									? ApprovalDecision.Rejected
									: ApprovalDecision.Approved,
								Comment = request.Status == LeaveStatus.ManagerRejected
									? "Rejected due to workload constraints."
									: "Approved. Please ensure handover.",
								ActionedAt = request.StartDate.AddHours(-rand.Next(4, 72))
							});
						}
					}
				}

				await context.SaveChangesAsync();
			}

		}

		private static int CalculateWorkingDays(DateTime start, DateTime end, List<DateTime> holidays)
		{
			int days = 0;

			for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
			{
				if (date.DayOfWeek != DayOfWeek.Saturday &&
					date.DayOfWeek != DayOfWeek.Sunday &&
					!holidays.Contains(date))
				{
					days++;
				}
			}

			return days;
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
