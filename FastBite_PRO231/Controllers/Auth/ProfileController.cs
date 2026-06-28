using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Controllers.Auth
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
