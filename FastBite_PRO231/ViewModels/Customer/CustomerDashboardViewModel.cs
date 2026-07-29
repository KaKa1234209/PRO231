using System;
using System.Collections.Generic;

namespace FastBite_PRO231.ViewModels;

public class CustomerDashboardViewModel
{
    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = "";

    public string UserName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";

    public int Point { get; set; }

    public int CartItemCount { get; set; }

    public int TotalOrders { get; set; }

    public int PendingOrders { get; set; }

    public int ProcessingOrders { get; set; }

    public int CompletedOrders { get; set; }

    public int CancelledOrders { get; set; }

    public decimal TotalSpent { get; set; }

    public List<CustomerDashboardOrderViewModel> RecentOrders
    { get; set; } = new();

    public List<CustomerDashboardPromotionViewModel> Promotions
    { get; set; } = new();
}

public class CustomerDashboardOrderViewModel
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "";

    public int TotalQuantity { get; set; }

    public bool HasInvoice { get; set; }
}

public class CustomerDashboardPromotionViewModel
{
    public int PromotionId { get; set; }

    public string PromotionName { get; set; } = "";

    public string DiscountType { get; set; } = "";

    public decimal DiscountValue { get; set; }

    public int ProductCount { get; set; }

    public string ProductNames { get; set; } = "";
}