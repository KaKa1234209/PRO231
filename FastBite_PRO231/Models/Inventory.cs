using System;
using System.Collections.Generic;

namespace FastBite_PRO231.Models;

public partial class Inventory
{
    public int InventoryId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public string Unit { get; set; } = null!;

    public DateTime UpdateAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
