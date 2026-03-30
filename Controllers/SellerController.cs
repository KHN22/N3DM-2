using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;
using N3DMMarket.Models.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using N3DMMarket.Filters;

namespace Marketplace.Controllers
{
    [RequireRoles("Seller,Admin")]
    public class SellerController : Controller
    {
        private readonly ThreedmContext _db;
        private readonly IWebHostEnvironment _env;

        public SellerController(ThreedmContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }
        // GET: /Seller or /Seller/Index
        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Products));

            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == email.Trim().ToUpper());
            if (user == null) return RedirectToAction(nameof(Products));

            var sellerId = user.UserId;

            // Total products and active products
            var totalProducts = _db.Products.Count(p => p.SellerId == sellerId);
            var activeProducts = _db.Products.Count(p => p.SellerId == sellerId && p.IsPublished);

            // Sales, earnings and views derived from order items
            var orderItems = _db.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == sellerId);

            var totalSales = orderItems.Sum(oi => (int?)oi.Quantity) ?? 0;
            var totalEarnings = orderItems.Sum(oi => (decimal?)oi.LineTotal) ?? 0m;

            // Pending balance: treat orders with PaymentStatus != "Completed" as pending
            var pendingBalance = _db.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.PaymentStatus != "Completed")
                .Sum(oi => (decimal?)oi.LineTotal) ?? 0m;

            // Views & conversion rate are not tracked per product in the DB currently; use zeros/defaults
            var totalViews = 0;
            var conversionRate = 0m;

            var dashboard = new SellerDashboardViewModel
            {
                TotalEarnings = totalEarnings,
                PendingBalance = pendingBalance,
                TotalSales = totalSales,
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                TotalViews = totalViews,
                ConversionRate = conversionRate
            };

            return View(dashboard);
        }

        // GET: /Seller/Products
        public IActionResult Products()
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return View(new List<SellerProductRow>());

            var normalized = email.Trim().ToUpper();
            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == normalized);
            if (user == null) return View(new List<SellerProductRow>());

            var products = _db.Products
                .Where(p => p.SellerId == user.UserId)
                .Select(p => new SellerProductRow
                {
                    Id = p.ProductId,
                    Title = p.Title,
                    Category = p.Category,
                    Price = p.Price,
                    Sales = _db.OrderItems.Where(oi => oi.ProductId == p.ProductId).Sum(oi => (int?)oi.Quantity) ?? 0,
                    Views = 0,
                    Revenue = _db.OrderItems.Where(oi => oi.ProductId == p.ProductId).Sum(oi => (decimal?)oi.LineTotal) ?? 0m,
                    Status = p.IsPublished ? "Active" : "Draft",
                    CreatedDate = p.CreatedAt
                })
                .OrderByDescending(p => p.CreatedDate)
                .ToList();

            return View(products);
        }

        // GET: /Seller/Create
        public IActionResult Create()
        {
            return View(new ProductFormViewModel());
        }

        // POST: /Seller/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Products));

            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == email.Trim().ToUpper());
            if (user == null) return RedirectToAction(nameof(Products));

            var product = new Product
            {
                Title = model.Title ?? string.Empty,
                Price = model.Price,
                Category = model.Category ?? string.Empty,
                ThumbnailUrl = model.ThumbnailUrl ?? string.Empty,
                IsPublished = string.Equals(model.Status, "Active", StringComparison.OrdinalIgnoreCase),
                Stock = model.Stock,
                SellerId = user.UserId,
                CreatedAt = DateTime.UtcNow
            };

            // Handle thumbnail upload (form field name: ThumbnailFile)
            var thumb = Request.Form.Files.FirstOrDefault(f => f.Name == "ThumbnailFile");
            if (thumb != null && thumb.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath ?? "", "uploads", "products", user.UserId.ToString());
                Directory.CreateDirectory(uploadsRoot);
                var ext = Path.GetExtension(thumb.FileName);
                var fileName = $"thumb_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    thumb.CopyTo(stream);
                }
                product.ThumbnailUrl = $"/uploads/products/{user.UserId}/{fileName}";
            }

            _db.Products.Add(product);
            _db.SaveChanges();

            return RedirectToAction(nameof(Products));
        }

        // GET: /Seller/Edit/{id}
        public IActionResult Edit(int id)
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Products));

            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == email.Trim().ToUpper());
            if (user == null) return RedirectToAction(nameof(Products));

            var p = _db.Products.FirstOrDefault(x => x.ProductId == id && x.SellerId == user.UserId);
            if (p == null) return NotFound();

            var product = new ProductFormViewModel
            {
                Id = p.ProductId,
                Title = p.Title,
                Price = p.Price,
                Category = p.Category,
                ThumbnailUrl = p.ThumbnailUrl,
                Status = p.IsPublished ? "Active" : "Draft",
                Stock = p.Stock
            };
            return View("Create", product);
        }

        // POST: /Seller/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(ProductFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Collect ModelState errors for debugging and redirect back to edit form
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                var message = errors.Count == 0 ? "Invalid input" : string.Join("; ", errors);
                TempData["Error"] = "Validation failed: " + message;
                // preserve entered values in TempData if helpful
                return RedirectToAction("Edit", new { id = model.Id });
            }

            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Products));

            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == email.Trim().ToUpper());
            if (user == null) return RedirectToAction(nameof(Products));

            var product = _db.Products.FirstOrDefault(p => p.ProductId == model.Id && p.SellerId == user.UserId);
            if (product == null) return NotFound();

            product.Title = model.Title ?? product.Title;
            product.Price = model.Price;
            product.Category = model.Category ?? product.Category;
            product.ThumbnailUrl = model.ThumbnailUrl ?? product.ThumbnailUrl;
            product.IsPublished = string.Equals(model.Status, "Active", StringComparison.OrdinalIgnoreCase);
            product.Stock = model.Stock;

            // If a new thumbnail was uploaded, save it and replace the existing thumbnail
            var thumb = Request.Form.Files.FirstOrDefault(f => f.Name == "ThumbnailFile");
            if (thumb != null && thumb.Length > 0)
            {
                var uploadsRoot = Path.Combine(_env.WebRootPath ?? "", "uploads", "products", user.UserId.ToString());
                Directory.CreateDirectory(uploadsRoot);
                var ext = Path.GetExtension(thumb.FileName);
                var fileName = $"thumb_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    thumb.CopyTo(stream);
                }

                // Optionally delete old thumbnail file if it exists and is under our uploads folder
                try
                {
                    if (!string.IsNullOrEmpty(product.ThumbnailUrl) && product.ThumbnailUrl.StartsWith("/uploads/products/"))
                    {
                        var oldPath = product.ThumbnailUrl.Replace('/', Path.DirectorySeparatorChar);
                        // oldPath begins with \uploads..., combine with web root
                        var candidate = Path.Combine(_env.WebRootPath ?? "", oldPath.TrimStart(Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(candidate)) System.IO.File.Delete(candidate);
                    }
                }
                catch { }

                product.ThumbnailUrl = $"/uploads/products/{user.UserId}/{fileName}";
            }

            try
            {
                _db.SaveChanges();
                TempData["Success"] = "Product updated.";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = "Error saving product: " + ex.Message;
            }

            return RedirectToAction(nameof(Products));
        }

        // POST: /Seller/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Products));

            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == email.Trim().ToUpper());
            if (user == null) return RedirectToAction(nameof(Products));

            var product = _db.Products.FirstOrDefault(p => p.ProductId == id && p.SellerId == user.UserId);
            if (product == null) return NotFound();

            _db.Products.Remove(product);
            _db.SaveChanges();

            return RedirectToAction(nameof(Products));
        }

        // GET: /Seller/Orders
        public IActionResult Orders()
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return View(new List<SellerOrderRow>());

            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == email.Trim().ToUpper());
            if (user == null) return View(new List<SellerOrderRow>());

            var sellerId = user.UserId;

            // Find order items for this seller and group by Order
            var items = _db.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == sellerId)
                .ToList();

            var orders = items
                .GroupBy(oi => oi.OrderId)
                .Select(g => new SellerOrderRow
                {
                    OrderId = g.Key.ToString(),
                    ProductTitle = string.Join(", ", g.Select(x => x.TitleSnapshot).Distinct()),
                    BuyerName = g.First().Order?.User?.FullName ?? g.First().Order?.CustomerEmail ?? "Guest",
                    BuyerEmail = g.First().Order?.CustomerEmail ?? string.Empty,
                    Amount = g.Sum(x => x.LineTotal),
                    SellerEarnings = g.Sum(x => x.LineTotal),
                    OrderDate = g.First().Order?.CreatedAt ?? DateTime.UtcNow,
                    Status = g.First().Order?.Status ?? string.Empty
                })
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // GET: /Seller/Earnings
        public IActionResult Earnings()
        {
            var email = HttpContext.Session.GetString("CurrentUserEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Products));

            var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == email.Trim().ToUpper());
            if (user == null) return RedirectToAction(nameof(Products));

            var sellerId = user.UserId;

            // Sum completed order items for available balance
            var completed = _db.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.PaymentStatus == "Completed");

            var pending = _db.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.PaymentStatus != "Completed");

            var totalEarnings = completed.Sum(oi => (decimal?)oi.LineTotal) ?? 0m;
            var pendingBalance = pending.Sum(oi => (decimal?)oi.LineTotal) ?? 0m;

            // Lifetime earnings — all order items for seller
            var lifetime = _db.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == sellerId)
                .Sum(oi => (decimal?)oi.LineTotal) ?? 0m;

            var earnings = new SellerEarningsViewModel
            {
                TotalEarnings = totalEarnings,
                AvailableBalance = totalEarnings,
                PendingBalance = pendingBalance,
                LifetimeEarnings = lifetime,
                CommissionRate = 15m,
                PayoutMethod = user.Email ?? string.Empty,
                PayoutAccount = user.Email ?? string.Empty
            };

            return View(earnings);
        }

        // GET: /Seller/Analytics
        public IActionResult Analytics()
        {
            // In production, fetch from database
            var analytics = new SellerAnalyticsViewModel
            {
                TotalViews = 28540,
                UniqueVisitors = 18230,
                TotalSales = 847,
                ConversionRate = 2.97m,
                AvgOrderValue = 58.40m
            };
            return View(analytics);
        }

        // GET: /Seller/Settings
        public IActionResult Settings()
        {
            // In production, fetch from database
            var settings = new SellerSettingsViewModel
            {
                StoreName = "Creative 3D Studio",
                StoreDescription = "High-quality 3D models and assets.",
                ContactEmail = "contact@creative3dstudio.com",
                PayoutMethod = "PayPal",
                PaypalEmail = "seller@email.com"
            };
            return View(settings);
        }

        // POST: /Seller/UpdateStoreProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStoreProfile(SellerSettingsViewModel model)
        {
            // In production: update in database
            TempData["Success"] = "Store profile updated successfully.";
            return RedirectToAction(nameof(Settings));
        }

        // POST: /Seller/UpdatePayoutSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePayoutSettings(SellerSettingsViewModel model)
        {
            // In production: update in database
            TempData["Success"] = "Payout settings updated successfully.";
            return RedirectToAction(nameof(Settings));
        }

        // POST: /Seller/UpdateNotifications
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateNotifications(SellerSettingsViewModel model)
        {
            // In production: update in database
            TempData["Success"] = "Notification preferences updated.";
            return RedirectToAction(nameof(Settings));
        }
    }
}
