using System.Collections.Generic;

namespace FastBite_PRO231.ViewModels;

public class HomeIndexViewModel
{
    public int? SelectedCategoryId { get; set; }

    public int CategoryCount { get; set; }

    public int ProductCount { get; set; }

    public List<HomeCategoryItemViewModel> Categories { get; set; }
        = new();

    public List<HomeProductItemViewModel> Products { get; set; }
        = new();
}

public class HomeCategoryItemViewModel
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = "";

    public string Description { get; set; } = "";
}

public class HomeProductItemViewModel
{
    public int ProductId { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = "";

    public string ProductName { get; set; } = "";

    public string Description { get; set; } = "";

    public decimal Price { get; set; }

    public string ImageUrl { get; set; } = "";
}