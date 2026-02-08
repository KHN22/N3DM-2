using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;

namespace Marketplace.Controllers
{
    public class StoreController : Controller
    {
        // GET: /Store
        public IActionResult Index()
        {
            // Pass null — the view has static fallback cards for prototype
            return View(new List<ProductViewModel>());
        }
    }
}
