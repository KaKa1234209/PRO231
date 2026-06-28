using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Controllers.Admin
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
