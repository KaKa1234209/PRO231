using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Controllers.Admin
{
    public class AdminDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
