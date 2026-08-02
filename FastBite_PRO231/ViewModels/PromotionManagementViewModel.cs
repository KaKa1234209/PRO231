using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels;

public class PromotionManagementIndexViewModel
{
    public string Search { get; set; } = "";

    public string StatusFilter { get; set; } = "";

    public int TotalPromotions { get; set; }

    public int ActivePromotions { get; set; }

    public int PausedPromotions { get; set; }

    public int UpcomingPromotions { get; set; }

    public List<PromotionManagementItemViewModel> Promotions
    { get; set; } = new();
}

public class PromotionManagementItemViewModel
{
    public int PromotionId { get; set; }

    public string PromotionName { get; set; } = "";

    public string DiscountType { get; set; } = "";

    public decimal DiscountValue { get; set; }

    public string Status { get; set; } = "";

    public int ProductCount { get; set; }

    public string ProductNames { get; set; } = "";
}

public class PromotionManagementFormViewModel
{
    public int PromotionId { get; set; }

    [Required(
        ErrorMessage = "Vui lòng nhập tên khuyến mãi.")]
    [StringLength(
        100,
        ErrorMessage = "Tên khuyến mãi không được vượt quá 100 ký tự.")]
    [Display(Name = "Tên khuyến mãi")]
    public string PromotionName { get; set; } = "";

    [Required(
        ErrorMessage = "Vui lòng chọn loại giảm giá.")]
    [Display(Name = "Loại giảm giá")]
    public string DiscountType { get; set; } = "Percent";

    [Range(
        typeof(decimal),
        "0.01",
        "999999999",
        ErrorMessage = "Giá trị giảm phải lớn hơn 0.")]
    [Display(Name = "Giá trị giảm")]
    public decimal DiscountValue { get; set; }

    [Required(
        ErrorMessage = "Vui lòng chọn trạng thái.")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Đang hoạt động";

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
    [Display(Name = "Ngày kết thúc")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    public List<int> SelectedProductIds { get; set; }
        = new();

    public List<PromotionProductChoiceViewModel> Products
    { get; set; } = new();
}

public class PromotionProductChoiceViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string CategoryName { get; set; } = "";

    public decimal Price { get; set; }

    public string ImageUrl { get; set; } = "";

    public bool Status { get; set; }
}

public class PromotionManagementDetailsViewModel
{
    public int PromotionId { get; set; }

    public string PromotionName { get; set; } = "";

    public string DiscountType { get; set; } = "";

    public decimal DiscountValue { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    public string Status { get; set; } = "";

    public List<PromotionProductChoiceViewModel> Products
    { get; set; } = new();
}