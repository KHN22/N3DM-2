using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;
using N3DMMarket.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ThreedmContext _context;

        public ProfileController(ThreedmContext context)
        {
            _context = context;
        }

        // GET: /Profile
        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email))
            {
                var profile = new ProfileViewModel
                {
                    FullName = "Jane Doe",
                    Email = "jane@example.com",
                    Bio = "3D artist and VTuber enthusiast.",
                    Role = "Seller",
                    SellerStatus = "Approved",
                    AvatarInitials = "JD"
                };
                return View(profile);
            }

            var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var vm = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Bio = string.Empty,
                Role = user.Role?.RoleName ?? string.Empty,
                SellerStatus = string.Empty,
                AvatarInitials = "U"
            };

            return View(vm);
        }

        // POST: /Profile  (save profile)
        [HttpPost]
        public IActionResult Index(ProfileViewModel model)
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null) return RedirectToAction("Login", "Account");

            user.FullName = model.FullName;
            // profile fields that don't exist in DB are left out or you can add columns
            _context.Users.Update(user);
            _context.SaveChanges();

            return View(model);
        }

        // GET: /Profile/History
        public IActionResult History()
        {
            return View(new List<PurchaseViewModel>());
        }

        // GET: /Profile/Settings
        public IActionResult Settings()
        {
            return View();
        }
    }
}
