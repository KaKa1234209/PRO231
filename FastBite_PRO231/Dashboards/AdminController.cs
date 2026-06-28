using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Dashboards
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
