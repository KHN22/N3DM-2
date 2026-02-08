using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;

namespace Marketplace.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST: /Account/Login  (UI-only, no real auth)
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            // TODO: Replace with real authentication logic
            // For prototype, redirect to Home on submit
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register  (UI-only, no real auth)
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            // TODO: Replace with real registration logic
            // For prototype, redirect to email confirmation page
            return RedirectToAction("EmailConfirmation");
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

            // In a real app: verify user exists, create a secure token and send email.
            // For this prototype we create a demo token and show the reset link in the confirmation view.
            var token = System.Guid.NewGuid().ToString();
            var resetLink = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme);

            TempData["ResetLink"] = resetLink; // shown on confirmation page for testing

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

            // In a real app: verify token, update user's password securely.
            // For prototype we assume success.
            return RedirectToAction("ResetPasswordConfirmation");
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
