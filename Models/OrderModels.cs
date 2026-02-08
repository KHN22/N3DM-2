using System.ComponentModel.DataAnnotations;

namespace Marketplace.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class Order
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<CartItem> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Price * i.Quantity);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [EmailAddress]
        public string? CustomerEmail { get; set; }
    }
}
