using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels;

public class InvoiceManagementIndexViewModel
{
    public string Search { get; set; } = "";

    public string StatusFilter { get; set; } = "";

    public int TotalInvoices { get; set; }

    public int ActiveInvoices { get; set; }

    public int CancelledInvoices { get; set; }

    public decimal TotalRevenue { get; set; }

    public List<InvoiceManagementListItemViewModel> Invoices
    { get; set; } = new();
}

public class InvoiceManagementListItemViewModel
{
    public int InvoiceId { get; set; }

    public int OrderId { get; set; }

    public string CustomerName { get; set; } = "";

    public string EmployeeName { get; set; } = "";

    public DateTime InvoiceDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = "";

    public bool Status { get; set; }
}

public class InvoiceCreateViewModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Đơn hàng không hợp lệ.")]
    public int OrderId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Vui lòng chọn nhân viên.")]
    public int? EmployeeId { get; set; }

    [Required(
        ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    public string PaymentMethod { get; set; } = "Cash";

    public string CustomerName { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public List<InvoiceEmployeeOptionViewModel> Employees
    { get; set; } = new();

    public List<InvoiceLineViewModel> Items
    { get; set; } = new();
}

public class InvoiceEmployeeOptionViewModel
{
    public int EmployeeId { get; set; }

    public string FullName { get; set; } = "";
}

public class InvoiceDetailsViewModel
{
    public int InvoiceId { get; set; }

    public int OrderId { get; set; }

    public string CustomerName { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Email { get; set; } = "";

    public string Address { get; set; } = "";

    public string EmployeeName { get; set; } = "";

    public DateTime InvoiceDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = "";

    public bool Status { get; set; }

    public int TotalQuantity { get; set; }

    public List<InvoiceLineViewModel> Items
    { get; set; } = new();
}

public class InvoiceLineViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal SubTotal { get; set; }
}