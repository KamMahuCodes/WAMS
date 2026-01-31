using Microsoft.AspNetCore.Mvc;

namespace WAMS.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

		public IActionResult About()
		{
			return View();
		}

		//public IActionResult FAQ()
		//{
		//	return View();
		//}
	}
}
