using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels.Customer;

public class CustomerManagementIndexViewModel
{
    public string Search { get; set; } = "";

    public string StatusFilter { get; set; } = "";

    public int TotalCustomers { get; set; }

    public int ActiveCustomers { get; set; }

    public int InactiveCustomers { get; set; }

    public int CustomersWithOrders { get; set; }

    public int TotalPoints { get; set; }

    public List<CustomerManagementItemViewModel> Customers
    { get; set; } = new();
}

public class CustomerManagementItemViewModel
{
    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = "";

    public string UserName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";

    public int Point { get; set; }

    public string Status { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public int OrderCount { get; set; }

    public decimal TotalSpent { get; set; }
}

public class CustomerManagementDetailsViewModel
{
    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = "";

    public string UserName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";

    public int Point { get; set; }

    public string Status { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public int TotalOrders { get; set; }

    public int CompletedOrders { get; set; }

    public int CancelledOrders { get; set; }

    public decimal TotalSpent { get; set; }

    public List<CustomerOrderSummaryViewModel> RecentOrders
    { get; set; } = new();
}

public class CustomerOrderSummaryViewModel
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "";

    public bool HasInvoice { get; set; }
}

public class CustomerManagementEditViewModel
{
    public int CustomerId { get; set; }

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

    [StringLength(
        255,
        ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
    [Display(Name = "Địa chỉ")]
    public string Address { get; set; } = "";

    [Range(
        0,
        100000000,
        ErrorMessage = "Điểm tích lũy phải từ 0 trở lên.")]
    [Display(Name = "Điểm tích lũy")]
    public int Point { get; set; }

    [Required(
        ErrorMessage = "Vui lòng chọn trạng thái.")]
    [Display(Name = "Trạng thái tài khoản")]
    public string Status { get; set; } = "Hoạt động";
}