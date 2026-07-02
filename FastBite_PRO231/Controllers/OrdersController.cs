
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

    //Nhận đơn: Employee
    [HttpPost]
    public async Task<IActionResult> TakeOrder(int orderId)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId);

        var order = await _context.Orders.FindAsync(orderId);

        if (order.Status != "Chờ xử lý")
        {
            TempData["Error"] = "Không thể nhận đơn này";
            return RedirectToAction(nameof(Index));
        }

        if (employee == null)
        {
            return BadRequest("Tài khoản này không phải nhân viên");
        }

        if (order == null)
        {
            return NotFound();
        }

        if (order.EmployeeId != null)
        {
            TempData["Error"] = "Đơn hàng đã được nhận";
            return RedirectToAction(nameof(Index));
        }

        order.EmployeeId = employee.EmployeeId;
        order.Status = "Đang xử lý";

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    //Phân đơn: Admin
    [HttpPost]
    public async Task<IActionResult> AssignEmployee(int orderId, int employeeId)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order == null)
        {
            return NotFound();
        }

        if (order.Status != "Chờ xử lý")
        {
            TempData["Error"] = "Không thể nhận đơn này";
            return RedirectToAction(nameof(Index));
        }

        if (order.EmployeeId != null)
        {
            TempData["Error"] = "Đơn hàng đã có nhân viên xử lý";
            return RedirectToAction(nameof(Index));
        }

        order.EmployeeId = employeeId;
        order.Status = "Đang xử lý";

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
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
