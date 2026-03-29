using System;

namespace N3DMMarket.Models.Db;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public Guid OrderId { get; set; }

    public int ProductId { get; set; }

    public string TitleSnapshot { get; set; } = null!;

    public decimal PriceSnapshot { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
