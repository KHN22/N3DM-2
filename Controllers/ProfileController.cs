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

            var normalizedEmail = (email ?? string.Empty).Trim().ToUpper();
            var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Email.ToUpper() == normalizedEmail);
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

            var normalizedEmail = (email ?? string.Empty).Trim().ToUpper();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToUpper() == normalizedEmail);
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
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Account");

            var normalized = email.Trim().ToUpper();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToUpper() == normalized);
            if (user == null) return RedirectToAction("Login", "Account");

            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == user.UserId || (o.CustomerEmail != null && o.CustomerEmail.ToUpper() == normalized))
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var rows = new List<PurchaseViewModel>();
            foreach (var o in orders)
            {
                var title = "";
                if (o.OrderItems.Count == 1)
                {
                    title = o.OrderItems.First().TitleSnapshot;
                }
                else
                {
                    title = o.OrderItems.Count + " items";
                }

                rows.Add(new PurchaseViewModel
                {
                    OrderId = o.OrderId.ToString(),
                    ProductTitle = title,
                    Seller = string.Empty,
                    Amount = o.TotalAmount,
                    PurchaseDate = o.CreatedAt,
                    Status = o.Status ?? ""
                });
            }

            return View(rows);
        }

        // GET: /Profile/OrderDetails/{id}
        public IActionResult OrderDetails(string id)
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Account");

            if (!Guid.TryParse(id, out var guid)) return NotFound();

            var normalized = email.Trim().ToUpper();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToUpper() == normalized);
            if (user == null) return RedirectToAction("Login", "Account");

            var order = _context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.OrderId == guid);
            if (order == null) return NotFound();

            // ensure the order belongs to the current user (by user id or email)
            if (order.UserId.HasValue && user != null && order.UserId != user.UserId && (order.CustomerEmail ?? string.Empty).ToUpper() != normalized)
            {
                return Forbid();
            }

            var vm = new Marketplace.Models.PurchaseViewModel
            {
                OrderId = order.OrderId.ToString(),
                ProductTitle = order.OrderItems.Count == 1 ? order.OrderItems.First().TitleSnapshot : order.OrderItems.Count + " items",
                Amount = order.TotalAmount,
                PurchaseDate = order.CreatedAt,
                Status = order.Status ?? "",
                Seller = string.Empty
            };

            ViewData["OrderItems"] = order.OrderItems.ToList();
            return View(vm);
        }

        // GET: /Profile/Settings
        public IActionResult Settings()
        {
            return View();
        }
    }
}
