using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;
using Marketplace.Lib;

namespace Marketplace.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UsersRepository _usersRepo;

        public ProfileController(IWebHostEnvironment env)
        {
            _usersRepo = new UsersRepository(env.ContentRootPath);
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

            var user = _usersRepo.LoadAll().FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var vm = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Bio = user.Bio,
                Role = user.Role,
                SellerStatus = user.SellerStatus,
                AvatarInitials = user.AvatarInitials
            };

            return View(vm);
        }

        // POST: /Profile  (save profile)
        [HttpPost]
        public IActionResult Index(ProfileViewModel model)
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Account");

            var users = _usersRepo.LoadAll();
            var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null) return RedirectToAction("Login", "Account");

            user.FullName = model.FullName;
            user.Bio = model.Bio;
            user.SellerStatus = model.SellerStatus;
            user.AvatarInitials = model.AvatarInitials;

            _usersRepo.Save(user);

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
