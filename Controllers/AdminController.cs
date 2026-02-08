using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;

namespace Marketplace.Controllers
{
    public class AdminController : Controller
    {
        // GET: /Admin
        public IActionResult Index()
        {
            // Static mock data — view also has fallback values
            var dashboard = new AdminDashboardViewModel
            {
                TotalUsers = 1284,
                TotalSellers = 156,
                TotalProducts = 2847,
                TotalRevenue = 48320,
                RecentUsers = new List<AdminUserRow>()  // empty → view renders static rows
            };

            return View(dashboard);
        }

        // GET: /Admin/Users
        public IActionResult Users()
        {
            return View(new List<AdminUserRow>());
        }

        // GET: /Admin/SellerApprovals
        public IActionResult SellerApprovals()
        {
            return View(new List<SellerApprovalRow>());
        }

        // GET: /Admin/Sales
        public IActionResult Sales()
        {
            return View();
        }

        // GET: /Admin/Roles
        public IActionResult Roles()
        {
            return View();
        }
    }
}
