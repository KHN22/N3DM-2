using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;
using N3DMMarket.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Controllers
{
    public class AccountController : Controller
    {
        private readonly ThreedmContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountController(ThreedmContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users
                .FirstOrDefault(u => u.Email.Equals(model.Email ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                                     && u.Password == (model.Password ?? string.Empty));
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            HttpContext.Session.SetString("CurrentUserEmail", user.Email);
            return RedirectToAction("Index", "Profile");
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
                return View(model);
            }

            if (_context.Users.Any(u => u.Email.Equals(model.Email ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Email", "Email already registered");
                return View(model);
            }
            // Ensure the role exists (create if missing)
            var roleName = string.IsNullOrWhiteSpace(model.Role) ? "Buyer" : model.Role;
            var role = _context.Roles.FirstOrDefault(r => r.RoleName == roleName);
            if (role == null)
            {
                role = new Role { RoleName = roleName };
                _context.Roles.Add(role);
                _context.SaveChanges();
            }

            var user = new N3DMMarket.Models.Db.User
            {
                FullName = model.FullName ?? string.Empty,
                Email = model.Email ?? string.Empty,
                Password = model.Password ?? string.Empty,
                RoleId = role.RoleId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            HttpContext.Session.SetString("CurrentUserEmail", user.Email);

            return RedirectToAction("Index", "Profile");
        }

        // GET: /Account/EmailConfirmation
        public IActionResult EmailConfirmation()
        {
            return View();
        }

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = System.Guid.NewGuid().ToString();
            var resetLink = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme);

            TempData["ResetLink"] = resetLink;

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // GET: /Account/ForgotPasswordConfirmation
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        public IActionResult ResetPassword(string email, string token)
        {
            var vm = new ResetPasswordViewModel
            {
                Email = email ?? string.Empty,
                Token = token ?? string.Empty
            };
            return View(vm);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            return RedirectToAction("ResetPasswordConfirmation");
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("CurrentUserEmail");
            return RedirectToAction("Index", "Home");
        }

        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "U";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
        }
    }
}
