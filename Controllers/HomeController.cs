using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N3DMMarket.Models.Db;

namespace Marketplace.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home  or  /
        public IActionResult Index()
        {
            return View();
        }

        private readonly ThreedmContext _db;
        public HomeController(ThreedmContext db)
        {
            _db = db;
        }
        public IActionResult lab08()
        {
            var user = (from u in _db.Users
                        select u).ToList();
            return View(user);
        }

        // ส่วนที่ GET เอาไว้แสดง
        [HttpGet("Home/Lab09_10")]
        [HttpGet("Home/lab09-10")]
        public IActionResult Lab09_10()
        {
        
            var users = _db.Users.OrderByDescending(u => u.UserId).ToList();
            return View("lab09-10", users);
        }

        // ส่วนที่ POST เอาไว้สร้างในตอนแรก
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(string name, string? email)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Lab09_10");
            }

            // ตัว set default role ไม่ได้ทำให้แก้ไขได้
            var role = EnsureDefaultRole("User");

            var user = new User
            {
                FullName = name.Trim(),
                Email = email.Trim(),
                Password = Guid.NewGuid().ToString(),
                RoleId = role.RoleId,
                IsActive = true
            };
            _db.Users.Add(user);
            _db.SaveChanges();
            return RedirectToAction("Lab09_10");
        }

        public IActionResult EditUser(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null) return RedirectToAction("Lab09_10");
            return View("lab09-10-edit", user);
        }

        // ส่วนที่ POST เอาไว้แก้ไข
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUserPost(int userId, string name, string? email)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return RedirectToAction("Lab09_10");
            user.FullName = name;
            user.Email = email;
            _db.SaveChanges();
            return RedirectToAction("Lab09_10");
        }

        // ส่วนที่ POST ทำหน้าที่ลบ Search from DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == id);
            if (user != null)
            {
                _db.Users.Remove(user);
                _db.SaveChanges();
            }
            return RedirectToAction("Lab09_10");
        }

        private Role EnsureDefaultRole(string roleName)
        {
            var role = _db.Roles.FirstOrDefault(r => r.RoleName == roleName);
            if (role != null) return role;
            role = new Role { RoleName = roleName };
            _db.Roles.Add(role);
            _db.SaveChanges();
            return role;
        }
    }
}
