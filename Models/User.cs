using System;

namespace Marketplace.Models
{
    public class User
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // prototype: plain text
        public string Role { get; set; } = "Buyer";
        public string Bio { get; set; } = string.Empty;
        public string SellerStatus { get; set; } = string.Empty;
        public string AvatarInitials { get; set; } = "U";
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    }
}
