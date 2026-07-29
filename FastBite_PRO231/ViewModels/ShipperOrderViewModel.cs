namespace FastBite_PRO231.ViewModels;

public class ShipperHomeViewModel
{
    public string ShipperName { get; set; } = "";

    public int PendingClaimCount { get; set; }      // đơn chưa ai nhận
    public int MyActiveOrders { get; set; }          // đơn tôi đang giao, chưa hoàn thành
    public int TodayCompletedOrders { get; set; }    // đơn tôi hoàn thành hôm nay
    public decimal PendingSettlementAmount { get; set; } // tiền COD tôi thu, chưa nộp

    public List<ShipperOrderViewModel> MyOrders { get; set; } = new();
    public List<ShipperOrderViewModel> AvailableOrders { get; set; } = new();
}

public class ShipperOrderViewModel
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string Status { get; set; } = "";
}