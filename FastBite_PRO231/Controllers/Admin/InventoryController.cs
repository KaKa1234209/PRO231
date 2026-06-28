using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Controllers.Admin
{
    public class InventoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
