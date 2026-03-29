using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Controllers
{
    // Development helper routes to aid local testing only.
    public class DevController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public DevController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // GET: /Dev/Impersonate?email=admin@gmail.com
        // Sets session CurrentUserEmail for local testing. Only available in Development environment.
        public IActionResult Impersonate(string email)
        {
            if (!_env.IsDevelopment()) return NotFound();
            if (string.IsNullOrEmpty(email)) return BadRequest("email required");

            HttpContext.Session.SetString("CurrentUserEmail", email);
            return RedirectToAction("Roles", "Admin");
        }
    }
}
