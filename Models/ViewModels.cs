using System.ComponentModel.DataAnnotations;

namespace Marketplace.Models
{
    // ── Auth ──────────────────────────────────────────────

    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer";   // "Customer" or "Seller"
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ── Store / Products ─────────────────────────────────

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Seller { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public bool IsNew { get; set; }
    }

    // ── Profile ──────────────────────────────────────────

    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer";
        public string SellerStatus { get; set; } = string.Empty;  // "Pending" | "Approved" | ""
        public string AvatarInitials { get; set; } = "U";
    }

    // ── Purchase History ─────────────────────────────────

    public class PurchaseViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public string Seller { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Status { get; set; } = "Completed"; // "Completed" | "Pending" | "Refunded"
    }

    // ── Admin ────────────────────────────────────────────

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<AdminUserRow> RecentUsers { get; set; } = new();
        public List<int> MonthlySales { get; set; } = new();  // 12 month values for chart
    }

    public class AdminUserRow
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer";
        public string Status { get; set; } = "Active";
        public DateTime JoinedDate { get; set; }
    }

    public class SellerApprovalRow
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime AppliedDate { get; set; }
        public string Status { get; set; } = "Pending";  // "Pending" | "Approved" | "Rejected"
    }

    public class EditUserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // Admin should use Role Management page to change roles.
    }

    public class RolesPageViewModel
    {
        public List<N3DMMarket.Models.Db.Role> Roles { get; set; } = new();
        public List<AdminUserRow> Users { get; set; } = new();
    }
}
