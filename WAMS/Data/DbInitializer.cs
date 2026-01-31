using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
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

			string[] roles = { "Admin", "Employee", "Manager", "HR" };

			// 1. Create Roles
			foreach (var role in roles)
			{
				if (!await roleManager.RoleExistsAsync(role))
				{
					await roleManager.CreateAsync(new IdentityRole(role));
				}
			}

			// 2. Seed Users
			await SeedUserAsync(
				userManager,
				email: "admin@workflow.local",
				password: "Admin@123",
				fullName: "System Administrator",
				role: "Admin"
			);

			await SeedUserAsync(
				userManager,
				email: "employee@workflow.local",
				password: "Employee@123",
				fullName: "Default Employee",
				role: "Employee"
			);

			await SeedUserAsync(
				userManager,
				email: "manager@workflow.local",
				password: "Manager@123",
				fullName: "Default Manager",
				role: "Manager"
			);

			await SeedUserAsync(
				userManager,
				email: "hr@workflow.local",
				password: "HRAdmin@123",
				fullName: "HR Administrator",
				role: "HR"
			);
		}

		private static async Task SeedUserAsync(
			UserManager<User> userManager,
			string email,
			string password,
			string fullName,
			string role)
		{
			var user = await userManager.FindByEmailAsync(email);

			if (user == null)
			{
				user = new User
				{
					UserName = email,
					Email = email,
					FullName = fullName,
					EmailConfirmed = true
				};

				var result = await userManager.CreateAsync(user, password);

				if (!result.Succeeded)
				{
					var errors = string.Join(
						", ",
						result.Errors.Select(e => e.Description)
					);

					throw new Exception($"Failed to create user {email}: {errors}");
				}

				await userManager.AddToRoleAsync(user, role);
			}
		}
	}
}
