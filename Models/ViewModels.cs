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

    // ── Seller Panel View Models ─────────────────────────────────

    public class SellerDashboardViewModel
    {
        public decimal TotalEarnings { get; set; }
        public decimal PendingBalance { get; set; }
        public int TotalSales { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalViews { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class SellerProductRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Sales { get; set; }
        public int Views { get; set; }
        public decimal Revenue { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class ProductFormViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft"; // Draft | Active | Paused
        public int Stock { get; set; }
    }

    

    public class SellerEarningsViewModel
    {
        public decimal TotalEarnings { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal LifetimeEarnings { get; set; }
        public decimal CommissionRate { get; set; }
        public string PayoutMethod { get; set; } = string.Empty;
        public string PayoutAccount { get; set; } = string.Empty;
    }

    public class SellerAnalyticsViewModel
    {
        public int TotalViews { get; set; }
        public int UniqueVisitors { get; set; }
        public int TotalSales { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal AvgOrderValue { get; set; }
    }

    public class SellerSettingsViewModel
    {
        public string StoreName { get; set; } = string.Empty;
        public string StoreDescription { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string PayoutMethod { get; set; } = string.Empty;
        public string PaypalEmail { get; set; } = string.Empty;
        public bool EmailNotifications { get; set; } = true;
        public bool OrderNotifications { get; set; } = true;
        public bool ReviewNotifications { get; set; } = true;
        public bool WeeklySummary { get; set; } = true;
        public bool MarketingEmails { get; set; } = false;
    }

    public class PayoutHistoryRow
    {
        public string PayoutId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public DateTime RequestedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SellerOrderRow
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal SellerEarnings { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
