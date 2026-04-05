using System;

namespace N3DMMarket.Models.Db;

public partial class PromotionRedemption
{
    public int Id { get; set; }

    public int PromotionId { get; set; }

    public Guid? OrderId { get; set; }

    public int? UserId { get; set; }

    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;

    public decimal AmountApplied { get; set; }

    public string? Reference { get; set; }

    public virtual Promotion? Promotion { get; set; }
    public virtual Order? Order { get; set; }
}
