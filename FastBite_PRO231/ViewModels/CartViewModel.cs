namespace FastBite_PRO231.ViewModels;

//Thông tin tổng của giỏ hàng
public class CartViewModel
{
    public int CartId { get; set; }

    public int TotalQuantity { get; set; }

    public decimal TotalPrice { get; set; }

    public List<CartItemViewModel> Items { get; set; } = new();
}

//Thông tin từng dòng sản phẩm trong giỏ
public class CartItemViewModel
{
    public int CartItemId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public int Quantity { get; set; }

    public int StockQuantity { get; set; }

    public decimal Price { get; set; }

    public decimal SubTotal { get; set; }
}