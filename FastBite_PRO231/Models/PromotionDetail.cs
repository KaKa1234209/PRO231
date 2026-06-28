using System;
using System.Collections.Generic;

namespace FastBite_PRO231.Models;

public partial class PromotionDetail
{
    public int PromotionDetailId { get; set; }

    public int PromotionId { get; set; }

    public int ProductId { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}
