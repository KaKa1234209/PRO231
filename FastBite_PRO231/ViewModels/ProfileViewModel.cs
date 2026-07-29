using System;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels;

public class ProfileViewModel
{
    public int UserId { get; set; }

    public int? CustomerId { get; set; }

    public int? EmployeeId { get; set; }

    [Required(
        ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(
        100,
        ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
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
    [System.ComponentModel.DataAnnotations.EmailAddress]
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
    [Display(Name = "Địa chỉ nhận hàng")]
    public string Address { get; set; } = "";

    public string RoleName { get; set; } = "";

    public string AccountStatus { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public int Point { get; set; }

    public string Position { get; set; } = "";

    public DateTime? HireDate { get; set; }

    public string EmployeeStatus { get; set; } = "";

    public bool IsCustomer
    {
        get
        {
            return string.Equals(
                RoleName,
                "Customer",
                StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool IsEmployee
    {
        get
        {
            return string.Equals(
                       RoleName,
                       "Employee",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       RoleName,
                       "Empolyee",
                       StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool IsAdmin
    {
        get
        {
            return string.Equals(
                RoleName,
                "Admin",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}