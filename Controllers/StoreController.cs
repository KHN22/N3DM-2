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
            return View(viewOrder);
        }

        // POST: /Store/PaymentProcess
        [HttpPost]
        public IActionResult PaymentProcess(string id)
        {
            // Prototype: mark payment succeeded (no real gateway)
            return RedirectToAction("PaymentConfirmation", new { id });
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
