using System;
using System.Collections.Generic;

namespace FastBite_PRO231.ViewModels;

public class AdminDashboardViewModel
{
    //Doanh thu
    public decimal RevenueToday { get; set; }

    public decimal RevenueWeek { get; set; }

    public decimal RevenueMonth { get; set; }

    //Thống kê đơn hàng
    public int TotalOrders { get; set; }

    public int TodayOrders { get; set; }

    public int PendingOrders { get; set; }

    public int ProcessingOrders { get; set; }

    public int CompletedOrders { get; set; }

    public int CancelledOrders { get; set; }

    //Thống kê tổng quan
    public int TotalCustomers { get; set; }

    public int TotalProducts { get; set; }

    public int WorkingEmployees { get; set; }

    public int ActivePromotions { get; set; }

    //Cảnh báo tồn kho
    public int LowStockProducts { get; set; }

    public int OutOfStockProducts { get; set; }

    public List<AdminDashboardTopProductViewModel> TopProducts
    { get; set; } = new();

    public List<AdminDashboardRecentOrderViewModel> RecentOrders
    { get; set; } = new();

    public List<AdminDashboardStockWarningViewModel> StockWarnings
    { get; set; } = new();

    public List<AdminDashboardDailyRevenueViewModel> DailyRevenue
    { get; set; } = new();
}

public class AdminDashboardTopProductViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public decimal Revenue { get; set; }

    public decimal Percent { get; set; }
}

public class AdminDashboardRecentOrderViewModel
{
    public int OrderId { get; set; }

    public string CustomerName { get; set; } = "";

    public string EmployeeName { get; set; } = "";

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "";

    public bool HasInvoice { get; set; }
}

public class AdminDashboardStockWarningViewModel
{
    public int InventoryId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public string Unit { get; set; } = "";

    public string StatusText { get; set; } = "";
}

public class AdminDashboardDailyRevenueViewModel
{
    public DateTime Date { get; set; }

    public int InvoiceCount { get; set; }

    public decimal Revenue { get; set; }

    public decimal Percent { get; set; }
}