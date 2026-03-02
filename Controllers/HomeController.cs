using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;
using N3DMMarket.Models.Db;

namespace Marketplace.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home  or  /
        public IActionResult Index()
        {
            return View();
        }

        private readonly ThreedmContext _db;
        public HomeController(ThreedmContext db)
        {
            _db = db;
        }
        public IActionResult lab08()
        {
            var user = (from u in _db.Users
                        select u).ToList();
            return View(user);
        }
    }
}
