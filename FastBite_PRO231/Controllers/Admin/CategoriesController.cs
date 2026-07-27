
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FastBite_PRO231.Models;

public class CategoriesController : Controller
{
    private readonly FastBiteDbContext _context;

    public CategoriesController(FastBiteDbContext context)
    {
        _context = context;
    }

    // Trang chủ: Danh sách + tìm kiếm
    public async Task<IActionResult> Index(string? searchString)
    {
        var query = _context.Categories
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();

            query = query.Where(category =>
                category.CategoryName.Contains(searchString) ||
                (category.Description ?? "").Contains(searchString));
        }

        var categories = await query
            .OrderBy(category => category.CategoryId)
            .ToListAsync();

        ViewBag.SearchString = searchString;

        return View("~/Views/Admin/Category/Index.cshtml", categories);
    }

    // Xem chi tiết
    public async Task<IActionResult> Details(int? categoryid)
    {
        if (categoryid == null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(m => m.CategoryId == categoryid);
        if (category == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Category/Details.cshtml", category);
    }

    // Thêm
    [HttpGet]
    public IActionResult Create()
    {
        var category = new Category
        {
            CategoryName = "",
            Description = ""
        };

        return View("~/Views/Admin/Category/Create.cshtml", category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("CategoryName,Description")] Category category)
    {
        category.CategoryName = category.CategoryName?.Trim() ?? "";
        category.Description = category.Description?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(category.CategoryName))
        {
            ModelState.AddModelError(nameof(category.CategoryName), "Vui lòng nhập tên danh mục.");
        }

        var nameExists = await _context.Categories
            .AnyAsync(item => item.CategoryName == category.CategoryName);

        if (nameExists)
        {
            ModelState.AddModelError(nameof(category.CategoryName), "Tên danh mục đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Category/Create.cshtml", category);
        }

        category.Status = true;

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã thêm danh mục thành công.";

        return RedirectToAction(nameof(Index));
    }

    // Sửa
    [HttpGet]
    public async Task<IActionResult> Edit(int? categoryid)
    {
        if (categoryid == null)
        {
            return NotFound();
        }

        var category = await _context.Categories.FindAsync(categoryid);

        if (category == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Category/Edit.cshtml", category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? categoryid,
        [Bind("CategoryId,CategoryName,Description,Status")] Category category)
    {
        if (categoryid == null || categoryid.Value != category.CategoryId)
        {
            return NotFound();
        }

        category.CategoryName = category.CategoryName?.Trim() ?? "";
        category.Description = category.Description?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(category.CategoryName))
        {
            ModelState.AddModelError(nameof(category.CategoryName), "Vui lòng nhập tên danh mục.");
        }

        var nameExists = await _context.Categories
            .AnyAsync(item =>
                item.CategoryName == category.CategoryName &&
                item.CategoryId != category.CategoryId);

        if (nameExists)
        {
            ModelState.AddModelError(nameof(category.CategoryName), "Tên danh mục đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Category/Edit.cshtml", category);
        }

        try
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật danh mục.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Categories.AnyAsync(item => item.CategoryId == category.CategoryId))
            {
                return NotFound();
            }
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? categoryid)
    {
        if (categoryid == null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CategoryId == categoryid.Value);

        if (category == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Category/Delete.cshtml", category);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int categoryid)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(item => item.CategoryId == categoryid);

        if (category == null)
        {
            return NotFound();
        }

        var hasProducts = await _context.Products
            .AnyAsync(product => product.CategoryId == categoryid);

        if (hasProducts)
        {
            TempData["Error"] = "Danh mục đang có sản phẩm nên không thể xóa.";
            return RedirectToAction(nameof(Index));
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã xóa danh mục.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Stop(int? categoryid)
    {
        if (categoryid == null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CategoryId == categoryid.Value);

        if (category == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Category/Stop.cshtml", category);
    }

    [HttpPost, ActionName("Stop")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StopConfirmed(int categoryid)
    {
        var category = await _context.Categories
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.CategoryId == categoryid);

        if (category == null)
        {
            return NotFound();
        }

        category.Status = false;
        foreach (var product in category.Products)
        {
            product.Status = false;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã ngừng sử dụng danh mục.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CategoryExists(int categoryId)
    {
        return await _context.Categories.AnyAsync(item => item.CategoryId == categoryId);
    }

    private bool CategoryExists(int? categoryid)
    {
        return _context.Categories.Any(e => e.CategoryId == categoryid);
    }
}
