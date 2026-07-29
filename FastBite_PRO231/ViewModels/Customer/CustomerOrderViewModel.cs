namespace FastBite_PRO231.ViewModels.Customer;

public class CustomerOrderHistoryViewModel
{
    public string StatusFilter { get; set; } = "";

    public List<CustomerOrderListItemViewModel> Orders { get; set; }
        = new();
}

public class CustomerOrderListItemViewModel
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = "";

    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    public bool CanCancel { get; set; }
}

public class CustomerOrderDetailsViewModel
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = "";

    public decimal TotalAmount { get; set; }

    public int TotalQuantity { get; set; }

    public bool CanCancel { get; set; }

    public List<CustomerOrderDetailItemViewModel> Items { get; set; }
        = new();
}

public class CustomerOrderDetailItemViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal SubTotal { get; set; }
}