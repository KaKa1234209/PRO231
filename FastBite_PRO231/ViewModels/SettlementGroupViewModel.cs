namespace FastBite_PRO231.ViewModels;

public class SettlementGroupViewModel
{
    public int ShipperId { get; set; }
    public string ShipperName { get; set; } = "";
    public string ShipperPhone { get; set; } = "";
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; } // tổng tiền shipper cần nộp
    public List<int> OrderIds { get; set; } = new(); // dùng để submit bulk confirm
}

public class AssignShipperViewModel
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public int? CurrentShipperId { get; set; }
    public string? CurrentShipperName { get; set; }

    public List<ShipperOptionViewModel> AvailableShippers { get; set; } = new();
}

public class ShipperOptionViewModel
{
    public int ShipperId { get; set; }
    public string FullName { get; set; } = "";
    public int ActiveOrderCount { get; set; } // số đơn đang giao dở, để admin cân nhắc phân bổ đều
}

public class SettlementViewModel
{
    public List<SettlementGroupViewModel> Groups { get; set; } = new();
}