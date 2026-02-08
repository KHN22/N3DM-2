using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home  or  /
        public IActionResult Index()
        {
            return View();
        }
    }
}
