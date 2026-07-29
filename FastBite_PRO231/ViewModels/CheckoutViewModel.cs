using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels;

//Trang kiểm thông tin đặt hàng
public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
    public string Address { get; set; } = "";

    // Tọa độ lấy từ OpenStreetMap
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD";

    public string? Note { get; set; }

    public decimal DeliveryFee { get; set; }

    public List<CheckoutItemViewModel> Items { get; set; } = new();
}

//Từng dòng sản phẩm khi checkout
public class CheckoutItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

//Trang "Đặt hàng thành công"
public class OrderSuccessViewModel
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = "";

    public decimal TotalAmount { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public int TotalQuantity { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";

    public List<OrderSuccessItemViewModel> Items { get; set; } = new();
}

//Từng dòng sản phẩm khi thành công
public class OrderSuccessItemViewModel
{
    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal SubTotal { get; set; }
}