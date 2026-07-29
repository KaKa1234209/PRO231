using System;
using System.Collections.Generic;

namespace FastBite_PRO231.Models;

public partial class Shipper
{
    public int ShipperId { get; set; }

    public int UserId { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;
}
