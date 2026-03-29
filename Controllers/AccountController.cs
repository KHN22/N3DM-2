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
        private readonly ILogger<AccountController> _logger;

        public AccountController(ThreedmContext context, IWebHostEnvironment env, ILogger<AccountController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
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

            try
            {
                var normalizedEmail = (model.Email ?? string.Empty).Trim().ToUpper();
                var password = model.Password ?? string.Empty;
                var user = _context.Users
                    .FirstOrDefault(u => u.Email.ToUpper() == normalizedEmail && u.Password == password);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password");
                    return View(model);
                }

                HttpContext.Session.SetString("CurrentUserEmail", user.Email);
                return RedirectToAction("Index", "Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred while logging in. " + ex.Message);
                return View(model);
            }
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
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
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
                return View(model);
            }

            try
            {
                var normalizedEmail = (model.Email ?? string.Empty).Trim().ToUpper();
                if (_context.Users.Any(u => u.Email.ToUpper() == normalizedEmail))
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }

                // Ensure the role exists (create if missing)
                var roleName = string.IsNullOrWhiteSpace(model.Role) ? "Customer" : model.Role;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred while creating the account. " + ex.Message);
                return View(model);
            }
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.Remove("CurrentUserEmail");
                HttpContext.Session.Clear();
            }
            catch { }
            return RedirectToAction("Index", "Home");
        }

    }
}
