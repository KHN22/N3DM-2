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

        public StoreController(ThreedmContext db)
        {
            _db = db;
        }

        // GET: /Store
        public IActionResult Index()
        {
            var products = _db.Products
                .Select(p => new ProductViewModel
                {
                    Id = p.ProductId,
                    Title = p.Title,
                    Price = p.Price,
                    Category = p.Category,
                    ThumbnailUrl = p.ThumbnailUrl,
                    Seller = string.Empty,
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

        // POST: /Store/CheckoutConfirm
        [HttpPost]
        public IActionResult CheckoutConfirm(string? customerEmail)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("cart") ?? new List<CartItem>();
            if (cart.Count == 0) return RedirectToAction("Index");

            // create DB order
            var dbOrder = new N3DMMarket.Models.Db.Order
            {
                CustomerEmail = customerEmail,
                TotalAmount = cart.Sum(i => i.Price * i.Quantity),
                CreatedAt = DateTime.UtcNow,
                Status = "Completed",
                PaymentMethod = "Prototype",
                PaymentStatus = "Completed"
            };

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
            _db.SaveChanges();

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
