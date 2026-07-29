using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels;

public class InventoryManagementIndexViewModel
{
    public string Search { get; set; } = "";

    public string StockFilter { get; set; } = "";

    public int TotalInventories { get; set; }

    public int InStockProducts { get; set; }

    public int LowStockProducts { get; set; }

    public int OutOfStockProducts { get; set; }

    public int TotalQuantity { get; set; }

    public List<InventoryManagementItemViewModel> Inventories
    { get; set; } = new();
}

public class InventoryManagementItemViewModel
{
    public int InventoryId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string CategoryName { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public bool ProductStatus { get; set; }

    public int Quantity { get; set; }

    public string Unit { get; set; } = "";

    public DateTime UpdateAt { get; set; }

    public string StockStatus { get; set; } = "";
}

public class InventoryManagementDetailsViewModel
{
    public int InventoryId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string CategoryName { get; set; } = "";

    public string Description { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public decimal Price { get; set; }

    public bool ProductStatus { get; set; }

    public int Quantity { get; set; }

    public string Unit { get; set; } = "";

    public DateTime UpdateAt { get; set; }

    public string StockStatus { get; set; } = "";
}

public class InventoryManagementFormViewModel
{
    public int InventoryId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Vui lòng chọn sản phẩm.")]
    [Display(Name = "Sản phẩm")]
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    [Range(
        0,
        100000000,
        ErrorMessage = "Số lượng phải từ 0 trở lên.")]
    [Display(Name = "Số lượng tồn")]
    public int Quantity { get; set; }

    [Required(
        ErrorMessage = "Vui lòng nhập đơn vị tính.")]
    [StringLength(
        20,
        ErrorMessage = "Đơn vị không được vượt quá 20 ký tự.")]
    [Display(Name = "Đơn vị tính")]
    public string Unit { get; set; } = "Phần";

    public List<InventoryProductChoiceViewModel> Products
    { get; set; } = new();
}

public class InventoryProductChoiceViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string CategoryName { get; set; } = "";

    public decimal Price { get; set; }

    public bool Status { get; set; }
}