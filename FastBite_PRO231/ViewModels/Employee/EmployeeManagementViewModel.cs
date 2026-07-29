using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels.Employee;

public class EmployeeManagementIndexViewModel
{
    public string Search { get; set; } = "";

    public string StatusFilter { get; set; } = "";

    public int TotalEmployees { get; set; }

    public int WorkingEmployees { get; set; }

    public int ResignedEmployees { get; set; }

    public int ActiveAccounts { get; set; }

    public List<EmployeeManagementItemViewModel> Employees
    { get; set; } = new();
}

public class EmployeeManagementItemViewModel
{
    public int EmployeeId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = "";

    public string UserName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Position { get; set; } = "";

    public DateTime HireDate { get; set; }

    public string EmployeeStatus { get; set; } = "";

    public string AccountStatus { get; set; } = "";

    public int OrdersHandled { get; set; }

    public int InvoicesCreated { get; set; }
}

public class EmployeeManagementDetailsViewModel
{
    public int EmployeeId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = "";

    public string UserName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Position { get; set; } = "";

    public DateTime HireDate { get; set; }

    public string EmployeeStatus { get; set; } = "";

    public string AccountStatus { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public int TotalOrdersHandled { get; set; }

    public int CompletedOrders { get; set; }

    public int TotalInvoices { get; set; }

    public decimal ActiveInvoiceRevenue { get; set; }

    public List<EmployeeOrderSummaryViewModel> RecentOrders
    { get; set; } = new();

    public List<EmployeeInvoiceSummaryViewModel> RecentInvoices
    { get; set; } = new();
}

public class EmployeeOrderSummaryViewModel
{
    public int OrderId { get; set; }

    public string CustomerName { get; set; } = "";

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "";
}

public class EmployeeInvoiceSummaryViewModel
{
    public int InvoiceId { get; set; }

    public int OrderId { get; set; }

    public DateTime InvoiceDate { get; set; }

    public decimal TotalAmount { get; set; }

    public bool Status { get; set; }
}

public class EmployeeManagementFormViewModel
{
    public int EmployeeId { get; set; }

    public int UserId { get; set; }

    [Required(
        ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(
        100,
        ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(
        50,
        MinimumLength = 3,
        ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(
        ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(
        255,
        ErrorMessage = "Email không được vượt quá 255 ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(
        @"^(0|\+84)[0-9]{9}$",
        ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng nhập chức vụ.")]
    [StringLength(
        50,
        ErrorMessage = "Chức vụ không được vượt quá 50 ký tự.")]
    [Display(Name = "Chức vụ")]
    public string Position { get; set; } = "";

    [DataType(DataType.Date)]
    [Display(Name = "Ngày vào làm")]
    public DateTime HireDate { get; set; } = DateTime.Today;

    [Required(
        ErrorMessage = "Vui lòng chọn trạng thái.")]
    [Display(Name = "Trạng thái làm việc")]
    public string Status { get; set; } = "Đang làm việc";

    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Compare(
        nameof(Password),
        ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = "";
}