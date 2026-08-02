using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels;

public class ShipperManagementItemViewModel
{
    public int ShipperId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string ShipperStatus { get; set; } = "";
    public string AccountStatus { get; set; } = "";
    public int OrdersHandled { get; set; }
    public int CompletedOrders { get; set; }
}

public class ShipperManagementIndexViewModel
{
    public string Search { get; set; } = "";
    public string StatusFilter { get; set; } = "";

    public int TotalShippers { get; set; }
    public int WorkingShippers { get; set; }
    public int ResignedShippers { get; set; }
    public int ActiveAccounts { get; set; }

    public List<ShipperManagementItemViewModel> Shippers { get; set; } = new();
}

public class ShipperOrderSummaryViewModel
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
}

public class ShipperManagementDetailsViewModel
{
    public int ShipperId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string ShipperStatus { get; set; } = "";
    public string AccountStatus { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public int TotalOrdersHandled { get; set; }
    public int CompletedOrders { get; set; }

    // Tổng tiền COD đã thu nhưng CHƯA đối soát về cửa hàng
    public decimal PendingSettlementAmount { get; set; }

    public List<ShipperOrderSummaryViewModel> RecentOrders { get; set; } = new();
}

public class ShipperManagementFormViewModel
{
    public int ShipperId { get; set; }
    public int UserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    public string Status { get; set; } = "";

    public string Password { get; set; } = "";

    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = "";
}