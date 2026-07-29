namespace FastBite_PRO231.ViewModels;

public class OrderManagementIndexViewModel
{
    public string Search { get; set; } = "";

    public string StatusFilter { get; set; } = "";

    public int TotalOrders { get; set; }

    public int PendingOrders { get; set; }

    public int ProcessingOrders { get; set; }

    public int CompletedOrders { get; set; }

    public List<OrderManagementListItemViewModel> Orders { get; set; }
        = new();
}

public class OrderManagementListItemViewModel
{
    public int OrderId { get; set; }

    public string CustomerName { get; set; } = "";

    public string Phone { get; set; } = "";

    public string EmployeeName { get; set; } = "";

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = "";

    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }
}

public class OrderManagementDetailsViewModel
{
    public int OrderId { get; set; }

    public string CustomerName { get; set; } = "";

    public string UserName { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Email { get; set; } = "";

    public string Address { get; set; } = "";

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string EmployeeName { get; set; } = "";

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = "";

    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    public bool HasInvoice { get; set; }

    public List<OrderManagementDetailItemViewModel> Items { get; set; }
        = new();
}

public class OrderManagementDetailItemViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal SubTotal { get; set; }
}