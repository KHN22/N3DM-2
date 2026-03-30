using Microsoft.AspNetCore.Mvc;
using Marketplace.Models;
using N3DMMarket.Filters;
using N3DMMarket.Models.Db;
using Microsoft.EntityFrameworkCore;


namespace Marketplace.Controllers
{
    [RequireRoles("Admin,Moderator")]
    public class AdminController : Controller
    {
        private readonly ThreedmContext _db;

        public AdminController(ThreedmContext db)
        {
            _db = db;
        }
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
            var users = _db.Users.Include(u => u.Role).OrderByDescending(u => u.CreatedDate).Take(200)
                .Select(u => new AdminUserRow
                {
                    Id = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role.RoleName,
                    Status = (u.IsActive ?? true) ? "Active" : "Suspended",
                    JoinedDate = u.CreatedDate ?? DateTime.UtcNow
                })
                .ToList();

            return View(users);
        }

        // GET: /Admin/EditUser/5
        public IActionResult EditUser(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null) return NotFound();

            var vm = new EditUserViewModel
            {
                Id = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
            };

            return View(vm);
        }

        // POST: /Admin/EditUser
        [HttpPost]
        public IActionResult EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid) { return View(model); }

            var user = _db.Users.FirstOrDefault(u => u.UserId == model.Id);
            if (user == null) return NotFound();

            // Only allow editing name and email here
            user.FullName = model.FullName ?? string.Empty;
            user.Email = model.Email ?? string.Empty;

            _db.SaveChanges();
            return RedirectToAction("Users");
        }

        // GET: /Admin/SellerApprovals
        public IActionResult SellerApprovals()
        {
            // list users who requested to become sellers (role = SellerPending)
            var pendingRole = _db.Roles.FirstOrDefault(r => r.RoleName == "SellerPending");
            var sellerRole = _db.Roles.FirstOrDefault(r => r.RoleName == "Seller");
            var rejectedRole = _db.Roles.FirstOrDefault(r => r.RoleName == "SellerRejected");

            var pending = new List<SellerApprovalRow>();
            if (pendingRole != null)
            {
                pending = _db.Users.Where(u => u.RoleId == pendingRole.RoleId)
                    .Select(u => new SellerApprovalRow
                    {
                        Id = u.UserId,
                        FullName = u.FullName,
                        Email = u.Email,
                        AppliedDate = u.CreatedDate ?? DateTime.UtcNow,
                        Status = "Pending"
                    })
                    .OrderBy(u => u.AppliedDate)
                    .ToList();
            }

            // compute all-time counts
            ViewBag.PendingCount = pendingRole == null ? 0 : _db.Users.Count(u => u.RoleId == pendingRole.RoleId);
            ViewBag.ApprovedCount = sellerRole == null ? 0 : _db.Users.Count(u => u.RoleId == sellerRole.RoleId);
            ViewBag.RejectedCount = rejectedRole == null ? 0 : _db.Users.Count(u => u.RoleId == rejectedRole.RoleId);

            return View(pending);
        }

        [HttpPost]
        public IActionResult ApproveSeller(int userId)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return NotFound();

            var sellerRole = _db.Roles.FirstOrDefault(r => r.RoleName == "Seller");
            if (sellerRole == null) return BadRequest("Seller role not found");

            user.RoleId = sellerRole.RoleId;
            _db.SaveChanges();
            return RedirectToAction("SellerApprovals");
        }

        [HttpPost]
        public IActionResult RejectSeller(int userId)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return NotFound();

            var rejectedRole = _db.Roles.FirstOrDefault(r => r.RoleName == "SellerRejected");
            if (rejectedRole == null)
            {
                rejectedRole = new Role { RoleName = "SellerRejected" };
                _db.Roles.Add(rejectedRole);
                _db.SaveChanges();
            }

            user.RoleId = rejectedRole.RoleId;
            _db.SaveChanges();
            return RedirectToAction("SellerApprovals");
        }

        // GET: /Admin/Sales
        public IActionResult Sales()
        {
            return View();
        }

        // GET: /Admin/Roles
        [RequireRoles("Admin")]
        public IActionResult Roles()
        {
            var roles = _db.Roles.OrderBy(r => r.RoleName).ToList();
            var users = _db.Users.Include(u => u.Role)
                .Where(u => u.Email == null || !EF.Functions.Like(u.Email, "%@example.com"))
                .OrderByDescending(u => u.CreatedDate).Take(500)
                .Select(u => new AdminUserRow
                {
                    Id = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role.RoleName,
                    Status = (u.IsActive ?? true) ? "Active" : "Suspended",
                    JoinedDate = u.CreatedDate ?? DateTime.UtcNow
                })
                .ToList();

            var vm = new Marketplace.Models.RolesPageViewModel
            {
                Roles = roles,
                Users = users
            };

            return View(vm);
        }

        [HttpPost]
        [RequireRoles("Admin")]
        public IActionResult UpdateRole(int userId, int roleId)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return NotFound();

            var role = _db.Roles.FirstOrDefault(r => r.RoleId == roleId);
            if (role == null) return BadRequest("Invalid role");

            user.RoleId = roleId;
            _db.SaveChanges();
            return RedirectToAction("Roles");
        }
    }
}
