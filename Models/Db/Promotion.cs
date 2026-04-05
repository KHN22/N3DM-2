using System;
using System.Collections.Generic;

namespace N3DMMarket.Models.Db;

public enum PromotionType
{
    Percentage = 0,
    FixedAmount = 1,
    FreeShipping = 2,
    Bogo = 3
}

public partial class Promotion
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public PromotionType Type { get; set; }

    public decimal Value { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? MaxUses { get; set; }

    public int? MaxUsesPerUser { get; set; }

    public string? AppliesTo { get; set; }

    public string? Metadata { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<PromotionRedemption> Redemptions { get; set; } = new List<PromotionRedemption>();
}
