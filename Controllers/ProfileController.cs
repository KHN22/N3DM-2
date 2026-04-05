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

            var initials = GetInitials(user.FullName);
            var vm = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Bio = string.Empty,
                Role = user.Role?.RoleName ?? string.Empty,
                SellerStatus = string.Empty,
                AvatarInitials = initials,
                ProfileImagePath = user.ProfileImagePath
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

            // Handle profile image upload
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = $"{user.UserId}_{Guid.NewGuid()}_{model.ProfileImage.FileName}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.ProfileImage.CopyTo(stream);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(user.ProfileImagePath))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                user.ProfileImagePath = $"/uploads/profiles/{fileName}";
            }

            _context.Users.Update(user);
            _context.SaveChanges();

            model.ProfileImagePath = user.ProfileImagePath;
            model.AvatarInitials = GetInitials(user.FullName);
            TempData["Success"] = "Profile updated successfully!";
            return View(model);
        }

        // POST: /Profile/UploadImage (AJAX upload)
        [HttpPost]
        public IActionResult UploadImage(IFormFile image)
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false, error = "Not logged in" });

            var normalizedEmail = (email ?? string.Empty).Trim().ToUpper();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToUpper() == normalizedEmail);
            if (user == null) return Json(new { success = false, error = "User not found" });

            if (image == null || image.Length == 0)
                return Json(new { success = false, error = "No image provided" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(image.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
                return Json(new { success = false, error = "Invalid file type" });

            try
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = $"{user.UserId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(user.ProfileImagePath))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                user.ProfileImagePath = $"/uploads/profiles/{fileName}";
                _context.Users.Update(user);
                _context.SaveChanges();

                return Json(new { success = true, imagePath = user.ProfileImagePath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "U";
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "U";
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return ($"{parts[0][0]}{parts[parts.Length - 1][0]}").ToUpper();
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

        // POST: /Profile/Settings (change password)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Account");

            var normalizedEmail = (email ?? string.Empty).Trim().ToUpper();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToUpper() == normalizedEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            // Validate inputs
            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                TempData["Error"] = "Current password is required.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                TempData["Error"] = "New password is required.";
                return View();
            }

            if (NewPassword.Length < 6)
            {
                TempData["Error"] = "New password must be at least 6 characters long.";
                return View();
            }

            if (NewPassword != ConfirmNewPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                return View();
            }

            if (CurrentPassword == NewPassword)
            {
                TempData["Error"] = "New password must be different from current password.";
                return View();
            }

            // Verify current password
            if (user.Password != CurrentPassword)
            {
                TempData["Error"] = "Current password is incorrect.";
                return View();
            }

            // Update password
            user.Password = NewPassword;
            _context.Users.Update(user);
            _context.SaveChanges();

            TempData["Success"] = "Password changed successfully!";
            return View();
        }
    }
}
