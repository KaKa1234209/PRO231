
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FastBite_PRO231.Models;

public class ProductsController : Controller
{
    private readonly FastBiteDbContext _context;

    public ProductsController(FastBiteDbContext context)
    {
        _context = context;
    }

    // Trang chủ: Danh sách + tìm kiếm
    public async Task<IActionResult> Index(string searchString)
    {
        var products = _context.Products
            .Include(c => c.Category)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            products = products.Where(c =>
                c.ProductName.Contains(searchString));
        }

        return View(await products.ToListAsync());
    }

    // Chi tiết
    public async Task<IActionResult> Details(int? productid)
    {
        if (productid == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.ProductId == productid);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // Thêm
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CategoryId,ProductName,Price,Description,Image,Status")] Product product)
    {
        if (ModelState.IsValid)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    // Sửa
    public async Task<IActionResult> Edit(int? productid)
    {
        if (productid == null)
        {
            return NotFound();
        }

        var product = await _context.Products.FindAsync(productid);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? productid, [Bind("CategoryId,ProductName,Price,Description,Image,Status")] Product product)
    {
        if (productid != product.ProductId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.ProductId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    // Xóa
    public async Task<IActionResult> Delete(int? productid)
    {
        if (productid == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.ProductId == productid);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? productid)
    {
        var product = await _context.Products.FindAsync(productid);
        if (product != null)
        {
            _context.Products.Remove(product);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(int? productid)
    {
        return _context.Products.Any(e => e.ProductId == productid);
    }

    //Cập nhật trạng thái
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int productId, bool status)
    {
        var product = await _context.Products.FindAsync(productId);

        if (product == null)
        {
            return NotFound();
        }

        product.Status = status;
        await _context.SaveChangesAsync();
            
        return Json(new { success = true });
    }
}
