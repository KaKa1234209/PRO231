using System;
using System.Collections.Generic;

namespace FastBite_PRO231.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public int? EmployeeId { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? Note { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string? TransactionId { get; set; }

    public DateTime? PaidAt { get; set; }

    public int? ShipperId { get; set; }

    public string SettlementStatus { get; set; } = null!;

    public DateTime? SettledAt { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public decimal DeliveryFee { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Shipper? Shipper { get; set; }
}
