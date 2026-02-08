using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;

namespace Marketplace.Controllers
{
    public class ProfileController : Controller
    {
        // GET: /Profile
        public IActionResult Index()
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

        // POST: /Profile  (save profile — UI only)
        [HttpPost]
        public IActionResult Index(ProfileViewModel model)
        {
            // TODO: persist changes
            return View(model);
        }

        // GET: /Profile/History
        public IActionResult History()
        {
            // Static mock data — view also has fallback rows
            return View(new List<PurchaseViewModel>());
        }

        // GET: /Profile/Settings
        public IActionResult Settings()
        {
            return View();
        }
    }
}
