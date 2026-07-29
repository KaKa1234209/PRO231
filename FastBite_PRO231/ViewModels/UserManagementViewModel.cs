using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels;

public class UserManagementIndexViewModel
{
    public string Search { get; set; } = "";

    public string RoleFilter { get; set; } = "";

    public string StatusFilter { get; set; } = "";

    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int InactiveUsers { get; set; }

    public int AdminUsers { get; set; }

    public int EmployeeUsers { get; set; }

    public int CustomerUsers { get; set; }

    public List<UserManagementItemViewModel> Users { get; set; }
        = new();
}

public class UserManagementItemViewModel
{
    public int UserId { get; set; }

    public string UserName { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Phone { get; set; } = "";

    public string RoleName { get; set; } = "";

    public string Status { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public bool HasCustomerProfile { get; set; }

    public bool HasEmployeeProfile { get; set; }
}

public class UserManagementDetailsViewModel
{
    public int UserId { get; set; }

    public string UserName { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Phone { get; set; } = "";

    public string RoleName { get; set; } = "";

    public string Status { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public int? CustomerId { get; set; }

    public string CustomerAddress { get; set; } = "";

    public int CustomerPoint { get; set; }

    public int? EmployeeId { get; set; }

    public string EmployeePosition { get; set; } = "";

    public DateTime? EmployeeHireDate { get; set; }

    public string EmployeeStatus { get; set; } = "";
}

public class UserManagementEditViewModel
{
    public int UserId { get; set; }

    [Required(
        ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(
        50,
        MinimumLength = 3,
        ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(
        100,
        ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

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

    public string RoleName { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng chọn trạng thái.")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Hoạt động";
}

public class AdminAccountCreateViewModel
{
    [Required(
        ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(
        50,
        MinimumLength = 3,
        ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(
        100,
        ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

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
        ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(
        6,
        ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(
        nameof(Password),
        ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = "";
}

public class AdminResetPasswordViewModel
{
    public int UserId { get; set; }

    public string UserName { get; set; } = "";

    public string FullName { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(
        6,
        ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = "";
}