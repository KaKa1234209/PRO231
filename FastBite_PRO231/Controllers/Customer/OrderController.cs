using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Controllers.Customer
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
