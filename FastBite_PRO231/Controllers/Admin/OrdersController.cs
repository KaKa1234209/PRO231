
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FastBite_PRO231.Models;

public class OrdersController : Controller
{
    private readonly FastBiteDbContext _context;

    public OrdersController(FastBiteDbContext context)
    {
        _context = context;
    }

    // Trang chủ: Danh sách + tìm kiếm
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Orders.ToListAsync());
    }

    // Chi tiết
    public async Task<IActionResult> Details(int? orderid)
    {
        if (orderid == null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(m => m.OrderId == orderid);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    //Cập nhật trạng thái
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int ordrId, bool status)
    {
        var order = await _context.Products.FindAsync(ordrId);

        if (order == null)
        {
            return NotFound();
        }

        order.Status = status;
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    //Hủy 
    [HttpPost]
    public async Task<IActionResult> Cancel(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order == null)
        {
            return NotFound();
        }

        order.Status = "Cancelled";

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
