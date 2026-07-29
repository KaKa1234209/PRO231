using System;
using System.Collections.Generic;

namespace FastBite_PRO231.ViewModels.Employee;

public class EmployeeDashboardViewModel
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = "";

    public string Position { get; set; } = "";

    public DateTime HireDate { get; set; }

    public int PendingStoreOrders { get; set; }

    public int AssignedOrders { get; set; }

    public int TodayAssignedOrders { get; set; }

    public int ProcessingOrders { get; set; }

    public int CompletedOrders { get; set; }

    public int TotalInvoices { get; set; }

    public decimal InvoiceRevenue { get; set; }

    public List<EmployeeDashboardOrderItemViewModel> WorkQueue
    { get; set; } = new();

    public List<EmployeeDashboardInvoiceItemViewModel> RecentInvoices
    { get; set; } = new();

    public List<EmployeeDashboardStockItemViewModel> StockWarnings
    { get; set; } = new();
}

public class EmployeeDashboardOrderItemViewModel
{
    public int OrderId { get; set; }

    public string CustomerName { get; set; } = "";

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "";

    public bool IsAssignedToMe { get; set; }

    public bool IsUnassigned { get; set; }

    public bool HasInvoice { get; set; }
}

public class EmployeeDashboardInvoiceItemViewModel
{
    public int InvoiceId { get; set; }

    public int OrderId { get; set; }

    public string CustomerName { get; set; } = "";

    public DateTime InvoiceDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = "";

    public bool Status { get; set; }
}

public class EmployeeDashboardStockItemViewModel
{
    public int InventoryId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public string Unit { get; set; } = "";

    public string StatusText { get; set; } = "";
}