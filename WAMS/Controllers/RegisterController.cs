using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WAMS.Models;
using WAMS.ViewModels;

namespace WAMS.Controllers
{
	public class RegisterController : Controller
    {
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;

		public RegisterController(
			SignInManager<User> signInManager,
			UserManager<User> userManager)
		{
			_signInManager = signInManager;
			_userManager = userManager;
		}
		// ============================
		// GET: /Auth/Register
		// ============================
		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		// ============================
		// POST: /Auth/Register
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(RegisterViewModel model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var user = new User
			{
				UserName = model.Email,
				Email = model.Email,
				FullName = model.FullName
			};

			var result = await _userManager.CreateAsync(user, model.Password);

			if (result.Succeeded)
			{
				// Assign default role
				await _userManager.AddToRoleAsync(user, "Employee");

				await _signInManager.SignInAsync(user, isPersistent: false);
				return RedirectToAction("Index", "Home");
			}

			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", error.Description);
			}

			return View(model);
		}

	}
}
