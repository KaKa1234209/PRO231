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
        //Lấy danh sách danh mục
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
        //Lấy danh sách sản phẩm
        var productQuery = _context.Products
            .AsNoTracking() //chỉ đọc dữ liệu, không theo dõi thay đổi
            .Include(product => product.Category)
            .Where(product => product.Status);

        if (categoryId.HasValue)
        {
            productQuery = productQuery.Where(product =>
                product.CategoryId == categoryId.Value);
        }

        //Sắp xếp 12 sản phẩm mới nhất lên đầu theo Id giảm
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
                //Ko có mô tả thì dung mặc định
                Description = string.IsNullOrWhiteSpace(
                    product.Description)
                    ? "Món ăn thơm ngon được chuẩn bị tại FastBite."
                    : product.Description,

                Price = product.Price,
                ImageUrl = NormalizeImageUrl(product.Image)
            })
            .ToList();
        //Đếm tổng số sản phẩm đang bán (không giới hạn 12, không lọc theo category) — dùng để hiển thị số liệu thống kê ở hero section.
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

    //Chuẩn hóa đường dẫn ảnh từ database thành URL dùng được trên web
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
}