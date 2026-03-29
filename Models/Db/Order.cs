using System;
using System.Collections.Generic;

namespace N3DMMarket.Models.Db;

public partial class Order
{
    public Guid OrderId { get; set; } = Guid.NewGuid();

    public int? UserId { get; set; }

    public string? CustomerEmail { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "Completed";

    public string PaymentMethod { get; set; } = "Prototype";

    public string PaymentStatus { get; set; } = "Completed";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User? User { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
