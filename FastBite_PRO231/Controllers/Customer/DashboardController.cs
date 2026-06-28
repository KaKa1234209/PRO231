using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
