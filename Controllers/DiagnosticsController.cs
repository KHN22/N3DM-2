using Microsoft.AspNetCore.Mvc;
using N3DMMarket.Models.Db;

namespace Marketplace.Controllers
{
    // Simple diagnostic endpoints to inspect session and role mapping
    public class DiagnosticsController : Controller
    {
        private readonly ThreedmContext _db;

        public DiagnosticsController(ThreedmContext db)
        {
            _db = db;
        }

        // GET: /diag/whoami
        [HttpGet]
        [Route("diag/whoami")]
        public IActionResult WhoAmI()
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail") ?? "(none)";
            try
            {
                if (string.IsNullOrEmpty(email) || email == "(none)")
                {
                    return Content($"Email: {email}\n", "text/plain");
                }

                var normalized = email.Trim().ToUpper();
                var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == normalized);
                if (user == null)
                {
                    return Content($"Email: {email}\nUser: not found\n", "text/plain");
                }

                var role = _db.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Email: {email}");
                sb.AppendLine($"UserId: {user.UserId}");
                sb.AppendLine($"RoleId (stored): {user.RoleId}");
                sb.AppendLine($"Role-nav: {(user.Role == null ? "(null)" : user.Role.RoleName)}");
                sb.AppendLine($"Role-lookup: {(role == null ? "(not found in Roles table)" : role.RoleName)}");
                sb.AppendLine();
                sb.AppendLine("Available roles:");
                foreach (var r in _db.Roles.OrderBy(r => r.RoleId))
                {
                    sb.AppendLine($"  {r.RoleId}: {r.RoleName}");
                }

                return Content(sb.ToString(), "text/plain");
            }
            catch (System.Exception ex)
            {
                return Content($"Email: {email}\nError reading DB: {ex.Message}", "text/plain");
            }
        }

        // POST: /diag/create-sample-product
        // Development-only helper to create a product for a given user email
        [HttpPost]
        [Route("diag/create-sample-product")]
        public IActionResult CreateSampleProduct(string email, string title = "Sample Product", decimal price = 9.99m)
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("email required");

            var normalized = email.Trim().ToUpper();
            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == normalized);
            if (user == null) return BadRequest("user not found");

            var product = new N3DMMarket.Models.Db.Product
            {
                Title = title,
                Price = price,
                SellerId = user.UserId,
                Category = "misc",
                ThumbnailUrl = "/images/placeholder-product.png",
                IsPublished = false,
                Stock = 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            _db.SaveChanges();

            return Created($"/products/{product.ProductId}", new { product.ProductId, product.Title });
        }

        // POST: /diag/create-test-seller
        // Creates a user (Seller) if missing, then creates a sample product for them.
        [HttpPost]
        [Route("diag/create-test-seller")]
        public IActionResult CreateTestSeller(string email, string fullName = "Test Seller", string password = "password")
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("email required");

            var normalized = email.Trim().ToUpper();
            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == normalized);

            // Ensure Seller role exists
            var sellerRole = _db.Roles.FirstOrDefault(r => r.RoleName == "Seller");
            if (sellerRole == null)
            {
                sellerRole = new N3DMMarket.Models.Db.Role { RoleName = "Seller" };
                _db.Roles.Add(sellerRole);
                _db.SaveChanges();
            }

            if (user == null)
            {
                user = new N3DMMarket.Models.Db.User
                {
                    FullName = fullName,
                    Email = email,
                    Password = password,
                    RoleId = sellerRole.RoleId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };
                _db.Users.Add(user);
                _db.SaveChanges();
            }
            else
            {
                // ensure role is seller
                user.RoleId = sellerRole.RoleId;
                _db.SaveChanges();
            }

            // create a sample product
            var product = new N3DMMarket.Models.Db.Product
            {
                Title = "Dev Sample Product",
                Price = 5.00m,
                SellerId = user.UserId,
                Category = "dev",
                ThumbnailUrl = "/images/placeholder-product.png",
                IsPublished = false,
                Stock = 1,
                CreatedAt = DateTime.UtcNow
            };
            _db.Products.Add(product);
            _db.SaveChanges();

            return Created($"/products/{product.ProductId}", new { product.ProductId, product.Title, user.UserId, user.Email });
        }
    }
}
