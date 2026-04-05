using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;
using Marketplace.Lib;
using N3DMMarket.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Controllers
{
    public class StoreController : Controller
    {
        private readonly ThreedmContext _db;
        private readonly ILogger<StoreController> _logger;

        public StoreController(ThreedmContext db, ILogger<StoreController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /Store
        public IActionResult Index()
        {
            var products = _db.Products
                .Include(p => p.Seller)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductViewModel
                {
                    Id = p.ProductId,
                    Title = p.Title,
                    Price = p.Price,
                    Category = p.Category,
                    ThumbnailUrl = string.IsNullOrEmpty(p.ThumbnailUrl) ? "/images/placeholder-product.png" : p.ThumbnailUrl,
                    Seller = p.Seller != null ? p.Seller.FullName : string.Empty,
                    Tags = new List<string>()
                })
                .ToList();

            return View(products);
        }

        // GET: /Store/Details/1
        public IActionResult Details(int id)
        {
            var p = _db.Products.FirstOrDefault(x => x.ProductId == id);
            if (p == null) return NotFound();
            var product = new ProductViewModel
            {
                Id = p.ProductId,
                Title = p.Title,
                Price = p.Price,
                Category = p.Category,
                ThumbnailUrl = p.ThumbnailUrl,
                Seller = string.Empty,
                Tags = new List<string>()
            };
            return View(product);
        }

        // POST: /Store/AddToCart
        [HttpPost]
        public IActionResult AddToCart(int id)
        {
            var p = _db.Products.FirstOrDefault(x => x.ProductId == id);
            if (p == null) return NotFound();

            var product = new ProductViewModel
            {
                Id = p.ProductId,
                Title = p.Title,
                Price = p.Price,
                Category = p.Category,
                ThumbnailUrl = p.ThumbnailUrl,
                Seller = string.Empty,
                Tags = new List<string>()
            };

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("cart") ?? new List<CartItem>();
            var existing = cart.FirstOrDefault(c => c.ProductId == id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                cart.Add(new CartItem { ProductId = id, Title = product.Title, Price = product.Price, Quantity = 1 });
            }
            HttpContext.Session.SetObjectAsJson("cart", cart);
            return RedirectToAction("Cart");
        }

        // GET: /Store/Cart
        public IActionResult Cart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("cart") ?? new List<CartItem>();
            return View(cart);
        }

        // POST: /Store/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("cart") ?? new List<CartItem>();
            cart.RemoveAll(c => c.ProductId == id);
            HttpContext.Session.SetObjectAsJson("cart", cart);
            return RedirectToAction("Cart");
        }

        // GET: /Store/Checkout
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("cart") ?? new List<CartItem>();
            if (cart.Count == 0) return RedirectToAction("Index");
            // Show checkout summary
            return View(cart);
        }

        // DEBUG: return current session cart as JSON (temporary)
        public IActionResult DebugCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("cart") ?? new List<CartItem>();
            return Json(new { count = cart.Count, items = cart });
        }

        // POST: /Store/CheckoutConfirm
        [HttpPost]
        public IActionResult CheckoutConfirm(string? customerEmail)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("cart") ?? new List<CartItem>();
            if (cart.Count == 0) return RedirectToAction("Index");

            // create DB order
            // prefer explicit email param, fallback to session email, else empty
            var sessionEmail = HttpContext.Session.GetString("CurrentUserEmail");
            var finalEmail = (customerEmail ?? sessionEmail ?? string.Empty).Trim();

            var dbOrder = new N3DMMarket.Models.Db.Order
            {
                CustomerEmail = finalEmail,
                TotalAmount = cart.Sum(i => i.Price * i.Quantity),
                CreatedAt = DateTime.UtcNow,
                Status = "Completed",
                PaymentMethod = "Prototype",
                PaymentStatus = "Completed"
            };

            // attach user if logged in (store user id on order for history)
            if (!string.IsNullOrEmpty(sessionEmail))
            {
                var user = _db.Users.FirstOrDefault(u => u.Email.ToUpper() == sessionEmail.Trim().ToUpper());
                if (user != null)
                {
                    dbOrder.UserId = user.UserId;
                }
            }

            foreach (var item in cart)
            {
                dbOrder.OrderItems.Add(new N3DMMarket.Models.Db.OrderItem
                {
                    ProductId = item.ProductId,
                    TitleSnapshot = item.Title ?? string.Empty,
                    PriceSnapshot = item.Price,
                    Quantity = item.Quantity,
                    LineTotal = item.Price * item.Quantity
                });
            }

            _db.Orders.Add(dbOrder);
            try
            {
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save order to database");
                var baseMsg = ex.GetBaseException()?.Message ?? ex.Message;
                TempData["Error"] = "Failed to create order: " + baseMsg;
                return RedirectToAction("Cart");
            }

            // Clear cart
            HttpContext.Session.Remove("cart");

            return RedirectToAction("Payment", new { id = dbOrder.OrderId.ToString() });
        }

        // GET: /Store/Payment/{orderId}
        public IActionResult Payment(string id)
        {
            if (!Guid.TryParse(id, out var guid)) return NotFound();
            var order = _db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.OrderId == guid);
            if (order == null) return NotFound();
            // map to view model Order (file-based) for existing views
            var viewOrder = new Marketplace.Models.Order
            {
                Id = order.OrderId.ToString(),
                CreatedAt = order.CreatedAt,
                CustomerEmail = order.CustomerEmail,
                Items = order.OrderItems.Select(oi => new Marketplace.Models.CartItem
                {
                    ProductId = oi.ProductId,
                    Title = oi.TitleSnapshot,
                    Price = oi.PriceSnapshot,
                    Quantity = oi.Quantity
                }).ToList()
            };
            // check for applied promotion in session
            var promoKey = $"appliedPromo:{order.OrderId}";
            var amountKey = $"appliedPromoAmount:{order.OrderId}";
            var promoIdStr = HttpContext.Session.GetString(promoKey);
            var amountStr = HttpContext.Session.GetString(amountKey);
            if (!string.IsNullOrEmpty(promoIdStr) && decimal.TryParse(amountStr, out var appliedAmount))
            {
                var promo = _db.Promotions.FirstOrDefault(p => p.Id.ToString() == promoIdStr);
                ViewBag.AppliedPromotionName = promo?.Name ?? "Promotion";
                ViewBag.AppliedPromotionAmount = appliedAmount;
            }
            else
            {
                ViewBag.AppliedPromotionName = null;
                ViewBag.AppliedPromotionAmount = 0m;
            }
            return View(viewOrder);
        }

        // GET: /Store/AvailablePromotions?orderId={orderId}
        public IActionResult AvailablePromotions(string orderId)
        {
            if (!Guid.TryParse(orderId, out var guid)) return Content(string.Empty);
            var order = _db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.OrderId == guid);
            if (order == null) return Content(string.Empty);

            var now = DateTime.UtcNow;
            var promos = _db.Promotions.Where(p => p.IsActive && (p.StartDate == null || p.StartDate <= now) && (p.EndDate == null || p.EndDate >= now)).ToList();

            var itemsTotal = order.OrderItems.Sum(i => i.PriceSnapshot * i.Quantity);
            var applicable = new List<Promotion>();
            foreach (var promo in promos)
            {
                if (promo.MinOrderAmount.HasValue && itemsTotal < promo.MinOrderAmount.Value) continue;

                // applies-to check (reuse logic from validation)
                if (!string.IsNullOrWhiteSpace(promo.AppliesTo) && !string.IsNullOrWhiteSpace(promo.Metadata))
                {
                    var applies = promo.AppliesTo.Trim();
                    var meta = promo.Metadata.Trim();
                    bool matched = false;
                    if (applies.Equals("Product", StringComparison.OrdinalIgnoreCase))
                    {
                        var ids = meta.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => int.TryParse(s, out var x) ? x : -1).Where(x => x > 0).ToHashSet();
                        if (order.OrderItems.Any(oi => ids.Contains(oi.ProductId))) matched = true;
                    }
                    else if (applies.Equals("Category", StringComparison.OrdinalIgnoreCase))
                    {
                        var cats = meta.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => s.ToLowerInvariant()).ToHashSet();
                        var productCategories = order.OrderItems.Join(_db.Products, oi => oi.ProductId, p => p.ProductId, (oi, p) => p.Category?.ToLowerInvariant() ?? string.Empty).ToHashSet();
                        if (productCategories.Any(c => cats.Contains(c))) matched = true;
                    }
                    else
                    {
                        matched = true;
                    }

                    if (!matched) continue;
                }

                // check usage limits
                if (promo.MaxUses.HasValue)
                {
                    var totalUsed = _db.PromotionRedemptions.Count(r => r.PromotionId == promo.Id);
                    if (totalUsed >= promo.MaxUses.Value) continue;
                }

                if (promo.MaxUsesPerUser.HasValue && order.UserId.HasValue)
                {
                    var usedByUser = _db.PromotionRedemptions.Count(r => r.PromotionId == promo.Id && r.UserId == order.UserId.Value);
                    if (usedByUser >= promo.MaxUsesPerUser.Value) continue;
                }

                applicable.Add(promo);
            }

            var vm = new Marketplace.Models.AvailablePromotionsViewModel
            {
                OrderId = orderId,
                Promotions = applicable
            };

            return PartialView("_AvailablePromotions", vm);
        }

        // POST: /Store/PaymentProcess
        [HttpPost]
        public IActionResult PaymentProcess(string id)
        {
            // Prototype: mark payment succeeded (no real gateway)
            // After payment, record promotion redemption if present in session
            if (Guid.TryParse(id, out var guid))
            {
                var order = _db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.OrderId == guid);
                if (order != null)
                {
                    var promoKey = $"appliedPromo:{order.OrderId}";
                    var amountKey = $"appliedPromoAmount:{order.OrderId}";
                    var promoIdStr = HttpContext.Session.GetString(promoKey);
                    var amountStr = HttpContext.Session.GetString(amountKey);
                    if (!string.IsNullOrEmpty(promoIdStr) && int.TryParse(promoIdStr, out var promoId) && !string.IsNullOrEmpty(amountStr) && decimal.TryParse(amountStr, out var appliedAmount))
                    {

                        var redemption = new PromotionRedemption
                        {
                            PromotionId = promoId,
                            OrderId = order.OrderId,
                            UserId = order.UserId,
                            AmountApplied = appliedAmount,
                            RedeemedAt = DateTime.UtcNow,
                            Reference = id
                        };

                        // link to order via GUID match if OrderId column intended to be int it may be null — save reference string instead
                        // store order id string in Reference and set OrderId = null for compatibility
                        _db.PromotionRedemptions.Add(redemption);
                        _db.SaveChanges();

                        HttpContext.Session.Remove(promoKey);
                        HttpContext.Session.Remove(amountKey);
                    }
                }
            }

            return RedirectToAction("PaymentConfirmation", new { id });
        }

        // POST: /Store/ValidatePromotion
        [HttpPost]
        public IActionResult ValidatePromotion(string code, string orderId)
        {
            if (string.IsNullOrWhiteSpace(code) || !Guid.TryParse(orderId, out var guid)) return Json(new { success = false, message = "Invalid request" });
            var promo = _db.Promotions.FirstOrDefault(p => p.Code != null && p.Code.ToUpper() == code.ToUpper() && p.IsActive);
            if (promo == null) return Json(new { success = false, message = "Promotion not found or inactive" });

            // verify date range
            var now = DateTime.UtcNow;
            if (promo.StartDate.HasValue && promo.StartDate > now) return Json(new { success = false, message = "Promotion not yet active" });
            if (promo.EndDate.HasValue && promo.EndDate < now) return Json(new { success = false, message = "Promotion expired" });

            var order = _db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.OrderId == guid);
            if (order == null) return Json(new { success = false, message = "Order not found" });

            var itemsTotal = order.OrderItems.Sum(i => i.PriceSnapshot * i.Quantity);

            if (promo.MinOrderAmount.HasValue && itemsTotal < promo.MinOrderAmount.Value) return Json(new { success = false, message = "Order does not meet minimum amount" });
            // check total uses
            if (promo.MaxUses.HasValue)
            {
                var totalUsed = _db.PromotionRedemptions.Count(r => r.PromotionId == promo.Id);
                if (totalUsed >= promo.MaxUses.Value) return Json(new { success = false, message = "Promotion has reached maximum uses" });
            }

            // check per-user uses
            int? userId = order.UserId;
            if (promo.MaxUsesPerUser.HasValue)
            {
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "Login required to use this promotion" });
                }
                var usedByUser = _db.PromotionRedemptions.Count(r => r.PromotionId == promo.Id && r.UserId == userId.Value);
                if (usedByUser >= promo.MaxUsesPerUser.Value) return Json(new { success = false, message = "You have already used this promotion the maximum number of times" });
            }

            // check applicability to products/categories
            if (!string.IsNullOrWhiteSpace(promo.AppliesTo) && !string.IsNullOrWhiteSpace(promo.Metadata))
            {
                var applies = promo.AppliesTo.Trim();
                var meta = promo.Metadata.Trim();
                bool matched = false;
                if (applies.Equals("Product", StringComparison.OrdinalIgnoreCase))
                {
                    // metadata: comma-separated product ids
                    var ids = meta.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => int.TryParse(s, out var x) ? x : -1).Where(x => x > 0).ToHashSet();
                    if (order.OrderItems.Any(oi => ids.Contains(oi.ProductId))) matched = true;
                }
                else if (applies.Equals("Category", StringComparison.OrdinalIgnoreCase))
                {
                    // metadata: comma-separated category names
                    var cats = meta.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => s.ToLowerInvariant()).ToHashSet();
                    var productCategories = order.OrderItems.Join(_db.Products, oi => oi.ProductId, p => p.ProductId, (oi, p) => p.Category?.ToLowerInvariant() ?? string.Empty).ToHashSet();
                    if (productCategories.Any(c => cats.Contains(c))) matched = true;
                }
                else
                {
                    matched = true; // unknown applies-to treated as all
                }

                if (!matched) return Json(new { success = false, message = "Promotion does not apply to items in your cart" });
            }

            // compute discount
            decimal discount = 0m;
            switch (promo.Type)
            {
                case PromotionType.Percentage:
                    discount = Math.Round(itemsTotal * (promo.Value / 100m), 2);
                    break;
                case PromotionType.FixedAmount:
                    discount = Math.Min(promo.Value, itemsTotal);
                    break;
                case PromotionType.FreeShipping:
                    discount = 0m; // shipping not modelled here
                    break;
                case PromotionType.Bogo:
                    discount = 0m; // implement later
                    break;
            }

            return Json(new { success = true, promotionId = promo.Id, amount = discount, name = promo.Name });
        }

        // POST: /Store/ApplyPromotion
        [HttpPost]
        public IActionResult ApplyPromotion(int promotionId, string orderId)
        {
            if (!Guid.TryParse(orderId, out var guid)) return RedirectToAction("Payment", new { id = orderId });
            var promo = _db.Promotions.FirstOrDefault(p => p.Id == promotionId && p.IsActive);
            if (promo == null) return RedirectToAction("Payment", new { id = orderId });

            var order = _db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.OrderId == guid);
            if (order == null) return RedirectToAction("Payment", new { id = orderId });

            var itemsTotal = order.OrderItems.Sum(i => i.PriceSnapshot * i.Quantity);
            // Reuse validation logic from ValidatePromotion by checking limits and applicability
            // check total uses
            if (promo.MaxUses.HasValue)
            {
                var totalUsed = _db.PromotionRedemptions.Count(r => r.PromotionId == promo.Id);
                if (totalUsed >= promo.MaxUses.Value)
                {
                    TempData["PromoError"] = "Promotion has reached maximum uses.";
                    return RedirectToAction("Payment", new { id = orderId });
                }
            }

            // check per-user uses
            int? userId = order.UserId;
            if (promo.MaxUsesPerUser.HasValue)
            {
                if (!userId.HasValue)
                {
                    TempData["PromoError"] = "Please log in to use this promotion.";
                    return RedirectToAction("Payment", new { id = orderId });
                }
                var usedByUser = _db.PromotionRedemptions.Count(r => r.PromotionId == promo.Id && r.UserId == userId.Value);
                if (usedByUser >= promo.MaxUsesPerUser.Value)
                {
                    TempData["PromoError"] = "You have already used this promotion the maximum number of times.";
                    return RedirectToAction("Payment", new { id = orderId });
                }
            }

            // check applicability
            if (!string.IsNullOrWhiteSpace(promo.AppliesTo) && !string.IsNullOrWhiteSpace(promo.Metadata))
            {
                var applies = promo.AppliesTo.Trim();
                var meta = promo.Metadata.Trim();
                bool matched = false;
                if (applies.Equals("Product", StringComparison.OrdinalIgnoreCase))
                {
                    var ids = meta.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => int.TryParse(s, out var x) ? x : -1).Where(x => x > 0).ToHashSet();
                    if (order.OrderItems.Any(oi => ids.Contains(oi.ProductId))) matched = true;
                }
                else if (applies.Equals("Category", StringComparison.OrdinalIgnoreCase))
                {
                    var cats = meta.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(s => s.ToLowerInvariant()).ToHashSet();
                    var productCategories = order.OrderItems.Join(_db.Products, oi => oi.ProductId, p => p.ProductId, (oi, p) => p.Category?.ToLowerInvariant() ?? string.Empty).ToHashSet();
                    if (productCategories.Any(c => cats.Contains(c))) matched = true;
                }
                else
                {
                    matched = true;
                }

                if (!matched)
                {
                    TempData["PromoError"] = "Promotion does not apply to items in your cart.";
                    return RedirectToAction("Payment", new { id = orderId });
                }
            }

            // compute discount
            decimal discount = 0m;
            switch (promo.Type)
            {
                case PromotionType.Percentage:
                    discount = Math.Round(itemsTotal * (promo.Value / 100m), 2);
                    break;
                case PromotionType.FixedAmount:
                    discount = Math.Min(promo.Value, itemsTotal);
                    break;
                default:
                    discount = 0m;
                    break;
            }

            order.TotalAmount = Math.Max(0, order.TotalAmount - discount);
            _db.SaveChanges();

            // store applied promo in session until payment completes
            HttpContext.Session.SetString($"appliedPromo:{order.OrderId}", promo.Id.ToString());
            HttpContext.Session.SetString($"appliedPromoAmount:{order.OrderId}", discount.ToString());

            return RedirectToAction("Payment", new { id = orderId });
        }

        // POST: /Store/ApplyPromotionByCode
        [HttpPost]
        public IActionResult ApplyPromotionByCode(string code, string orderId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["PromoError"] = "Please provide a promotion code.";
                return RedirectToAction("Payment", new { id = orderId });
            }

            var promo = _db.Promotions.FirstOrDefault(p => p.Code != null && p.Code.ToUpper() == code.ToUpper() && p.IsActive);
            if (promo == null)
            {
                TempData["PromoError"] = "Promotion not found or inactive.";
                return RedirectToAction("Payment", new { id = orderId });
            }

            return ApplyPromotion(promo.Id, orderId);
        }

        public IActionResult PaymentConfirmation(string id)
        {
            if (!Guid.TryParse(id, out var guid)) return NotFound();
            var order = _db.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.OrderId == guid);
            if (order == null) return NotFound();
            var viewOrder = new Marketplace.Models.Order
            {
                Id = order.OrderId.ToString(),
                CreatedAt = order.CreatedAt,
                CustomerEmail = order.CustomerEmail,
                Items = order.OrderItems.Select(oi => new Marketplace.Models.CartItem
                {
                    ProductId = oi.ProductId,
                    Title = oi.TitleSnapshot,
                    Price = oi.PriceSnapshot,
                    Quantity = oi.Quantity
                }).ToList()
            };
            return View(viewOrder);
        }
    }
}
