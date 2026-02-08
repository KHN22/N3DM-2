using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;
using Marketplace.Lib;

namespace Marketplace.Controllers
{
    public class StoreController : Controller
    {
        private readonly OrdersRepository _ordersRepo;
        private readonly IWebHostEnvironment _env;
        // In-memory sample products for prototype
        private static readonly List<ProductViewModel> _products = new()
        {
            new ProductViewModel { Id = 1, Title = "Sakura VTuber Avatar", Seller = "ArtistPro", Price = 24.99m, Category = "VTuber", ThumbnailUrl = "", Tags = new List<string>{"VTuber","Rigged","FBX"}, IsNew = true },
            new ProductViewModel { Id = 2, Title = "Mechanical Dragon - Print Ready", Seller = "MakerStudio", Price = 12.50m, Category = "3D Print", ThumbnailUrl = "", Tags = new List<string>{"3D Print","STL"} },
            new ProductViewModel { Id = 3, Title = "Cyberpunk Room Environment", Seller = "SceneForge", Price = 39.00m, Category = "Environment", ThumbnailUrl = "", Tags = new List<string>{"Environment","Unity","GLTF"} }
        };

        public StoreController(IWebHostEnvironment env)
        {
            _env = env;
            _ordersRepo = new OrdersRepository(env.ContentRootPath);
        }

        // GET: /Store
        public IActionResult Index()
        {
            return View(_products);
        }

        // GET: /Store/Details/1
        public IActionResult Details(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Store/AddToCart
        [HttpPost]
        public IActionResult AddToCart(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

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

            var order = new Order
            {
                Items = cart,
                CustomerEmail = customerEmail
            };
            _ordersRepo.Save(order);

            // Clear cart
            HttpContext.Session.Remove("cart");

            return RedirectToAction("Payment", new { id = order.Id });
        }

        // GET: /Store/Payment/{orderId}
        public IActionResult Payment(string id)
        {
            var orders = _ordersRepo.LoadAll();
            var order = orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
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
            var orders = _ordersRepo.LoadAll();
            var order = orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
