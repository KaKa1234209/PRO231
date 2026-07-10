using System.Diagnostics;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

public class HomeController : Controller
{
    private readonly FastBiteDbContext _context;

    public HomeController(FastBiteDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .Select(category => new HomeCategoryItemViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description ?? ""
            })
            .ToListAsync();

        var productQuery = _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.Status);

        if (categoryId.HasValue)
        {
            productQuery = productQuery.Where(product =>
                product.CategoryId == categoryId.Value);
        }

        var productEntities = await productQuery
            .OrderByDescending(product => product.ProductId)
            .Take(12)
            .ToListAsync();

        var products = productEntities
            .Select(product => new HomeProductItemViewModel
            {
                ProductId = product.ProductId,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.CategoryName
                    ?? "FastBite",

                ProductName = product.ProductName,

                Description = string.IsNullOrWhiteSpace(
                    product.Description)
                    ? "Món ăn thơm ngon được chuẩn bị tại FastBite."
                    : product.Description,

                Price = product.Price,
                ImageUrl = NormalizeImageUrl(product.Image)
            })
            .ToList();

        var model = new HomeIndexViewModel
        {
            SelectedCategoryId = categoryId,

            CategoryCount = categories.Count,

            ProductCount = await _context.Products
                .AsNoTracking()
                .CountAsync(product => product.Status),

            Categories = categories,
            Products = products
        };

        return View(model);
    }

    private static string NormalizeImageUrl(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            return "";
        }

        image = image.Trim();

        if (image.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) ||
            image.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            return image;
        }

        if (image.StartsWith("~/"))
        {
            return image[1..];
        }

        if (image.StartsWith("/"))
        {
            return image;
        }

        return $"/images/products/{image}";
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id
                ?? HttpContext.TraceIdentifier
        });
    }
}