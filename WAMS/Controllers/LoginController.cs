using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WAMS.Models;
using WAMS.Models.ViewModels;
using WAMS.ViewModels;

namespace WAMS.Controllers
{
	public class LoginController : Controller
	{
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;

		public LoginController(
			SignInManager<User> signInManager,
			UserManager<User> userManager)
		{
			_signInManager = signInManager;
			_userManager = userManager;
		}

		// ============================
		// GET: /Login
		// ============================
		[HttpGet]
		public IActionResult Login(string? returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

		// ============================
		// POST: /Auth/Login
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(
			Login model,
			string? returnUrl = null)
		{
			if (!ModelState.IsValid)
				return View(model);

			var user = await _userManager.FindByEmailAsync(model.Email);

			if (user == null)
			{
				ModelState.AddModelError("", "Invalid login attempt.");
				return View(model);
			}

			var result = await _signInManager.PasswordSignInAsync(
				user,
				model.Password,
				model.RememberMe,
				lockoutOnFailure: true
			);

			if (result.Succeeded)
			{
				if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
					return Redirect(returnUrl);

				return RedirectToAction("Index", "Dashboard");
			}

			if (result.IsLockedOut)
			{
				ModelState.AddModelError("", "Account locked. Try again later.");
				return View(model);
			}

			ModelState.AddModelError("", "Invalid login attempt.");
			return View(model);
		}

		// ============================
		// POST: /Auth/Logout
		// ============================
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction("Index", "Home");
		}
	}
}
