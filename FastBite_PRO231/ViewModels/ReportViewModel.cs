using System;
using System.Collections.Generic;

namespace FastBite_PRO231.ViewModels;

public class ReportViewModel
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public int TotalOrders { get; set; }

    public int PendingOrders { get; set; }

    public int ProcessingOrders { get; set; }

    public int CompletedOrders { get; set; }

    public int CancelledOrders { get; set; }

    public int TotalInvoices { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal AverageOrderValue { get; set; }

    public int TotalProductsSold { get; set; }

    public int LowStockProducts { get; set; }

    public int OutOfStockProducts { get; set; }

    public List<ReportDailyRevenueViewModel> DailyRevenue { get; set; }
        = new();

    public List<ReportTopSellingProductViewModel> TopProducts { get; set; }
        = new();

    public List<ReportTopCustomerViewModel> TopCustomers { get; set; }
        = new();

    public List<ReportLowStockViewModel> StockWarnings { get; set; }
        = new();
}

public class ReportDailyRevenueViewModel
{
    public DateTime Date { get; set; }

    public int InvoiceCount { get; set; }

    public decimal Revenue { get; set; }

    public decimal Percent { get; set; }
}

public class ReportTopSellingProductViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string CategoryName { get; set; } = "";

    public int QuantitySold { get; set; }

    public decimal Revenue { get; set; }

    public decimal Percent { get; set; }
}

public class ReportTopCustomerViewModel
{
    public int CustomerId { get; set; }

    public string FullName { get; set; } = "";

    public string Phone { get; set; } = "";

    public int OrderCount { get; set; }

    public decimal TotalSpent { get; set; }
}

public class ReportLowStockViewModel
{
    public int InventoryId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public string Unit { get; set; } = "";

    public string StockStatus { get; set; } = "";
}