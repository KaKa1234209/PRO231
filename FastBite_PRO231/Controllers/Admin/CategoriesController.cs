
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
    public async Task<IActionResult> Index(string searchString)
    {
        var categories = _context.Categories
            .Include(c => c.Products)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            searchString = searchString.Trim();

            categories = categories.Where(c =>
                c.CategoryName.Contains(searchString));
        }

        return View(await categories.ToListAsync());
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

        return View(category);
    }

    // Thêm
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CategoryName,Description")] Category category)
    {
        if (ModelState.IsValid)
        {
            category.Status = true;

            _context.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // Sửa
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
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? categoryid, [Bind("CategoryId,CategoryName,Description,Status")] Category category)
    {
        if (categoryid != category.CategoryId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật danh mục thành công.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.CategoryId))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // Xóa
    public async Task<IActionResult> Delete(int? categoryid)
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

        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? categoryid)
    {
        var category = await _context.Categories.FindAsync(categoryid);
        if (category == null)
        {
            return NotFound();
        }

        if (category.Products.Any())
        {
            ModelState.AddModelError("", "Danh mục đã có sản phẩm, không thể xóa.");
            return View(category);
        }

        _context.Categories.Remove(category);
        TempData["Success"] = "Xóa danh mục thành công.";

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Ngừng
    public async Task<IActionResult> Stop(int? categoryid)
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

        return View(category);
    }

    [HttpPost, ActionName("Stop")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StopConfirmed(int? categoryid)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryid);

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

    private bool CategoryExists(int? categoryid)
    {
        return _context.Categories.Any(e => e.CategoryId == categoryid);
    }
}
