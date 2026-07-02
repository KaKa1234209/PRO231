using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Controllers
{
    public class InventoriesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
