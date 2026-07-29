using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

public class ShipperHomeController : Controller
{
    private readonly FastBiteDbContext _context;

    public ShipperHomeController(FastBiteDbContext context)
    {
        _context = context;
    }

    private bool IsShipper()
    {
        var role = HttpContext.Session.GetString("Role");
        return string.Equals(role, "Shipper", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectToLogin()
    {
        TempData["Error"] = "Vui lòng đăng nhập bằng tài khoản shipper.";
        return RedirectToAction("Login", "Login");
    }

    private async Task<int?> GetShipperIdAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return null;

        var shipper = await _context.Shippers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId.Value);

        return shipper?.ShipperId;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsShipper()) return RedirectToLogin();

        var shipperId = await GetShipperIdAsync();
        if (!shipperId.HasValue)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ shipper.";
            return RedirectToLogin();
        }

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var allMyOrders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Where(o => o.ShipperId == shipperId.Value)
            .ToListAsync();

        var myActiveOrders = allMyOrders
            .Where(o => o.Status != "Hoàn thành" && o.Status != "Đã huỷ")
            .OrderBy(o => o.OrderDate)
            .Select(o => new ShipperOrderViewModel
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                CustomerName = o.Customer.User!.FullName ?? o.Customer.User.UserName ?? "Khách hàng",
                CustomerPhone = o.Customer.User!.Phone ?? "",
                DeliveryAddress = o.DeliveryAddress ?? "",
                Note = o.Note,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                Status = o.Status
            })
            .ToList();

        var availableOrders = _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Where(o =>
                o.ShipperId == null &&
                o.Status != "Hoàn thành" &&
                o.Status != "Đã huỷ" &&
                o.Status != "Chờ thanh toán")
            .OrderBy(o => o.OrderDate)
            .Select(o => new ShipperOrderViewModel
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                CustomerName = o.Customer.User!.FullName ?? o.Customer.User.UserName ?? "Khách hàng",
                CustomerPhone = o.Customer.User!.Phone ?? "",
                DeliveryAddress = o.DeliveryAddress ?? "",
                Note = o.Note,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                Status = o.Status
            })
            .ToList();

        var fullName = HttpContext.Session.GetString("FullName") ?? "Shipper";

        var model = new ShipperHomeViewModel
        {
            ShipperName = fullName,

            PendingClaimCount = availableOrders.Count,
            MyActiveOrders = myActiveOrders.Count,

            TodayCompletedOrders = allMyOrders.Count(o =>
                o.Status == "Hoàn thành" &&
                o.PaidAt.HasValue &&
                o.PaidAt.Value.Date == today),

            PendingSettlementAmount = allMyOrders
                .Where(o =>
                    o.PaymentMethod == "COD" &&
                    o.PaymentStatus == "Đã thanh toán" &&
                    o.SettlementStatus == "Chưa đối soát")
                .Sum(o => o.TotalAmount),

            MyOrders = myActiveOrders,
            AvailableOrders = availableOrders
        };

        return View("~/Views/ShipperHome/Index.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClaimOrder(int orderId)
    {
        if (!IsShipper()) return RedirectToLogin();

        var shipperId = await GetShipperIdAsync();
        if (!shipperId.HasValue) return RedirectToLogin();

        var affectedRows = await _context.Orders
            .Where(o =>
                o.OrderId == orderId &&
                o.ShipperId == null &&
                o.Status != "Hoàn thành" &&
                o.Status != "Đã huỷ" &&
                o.Status != "Chờ thanh toán")
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(o => o.ShipperId, shipperId.Value));

        TempData[affectedRows == 0 ? "Error" : "Success"] = affectedRows == 0
            ? "Đơn hàng này vừa được shipper khác nhận hoặc không còn khả dụng."
            : $"Bạn đã nhận đơn #{orderId}.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDelivered(int orderId)
    {
        if (!IsShipper()) return RedirectToLogin();

        var shipperId = await GetShipperIdAsync();
        if (!shipperId.HasValue) return RedirectToLogin();

        var order = await _context.Orders
            .FirstOrDefaultAsync(o =>
                o.OrderId == orderId &&
                o.ShipperId == shipperId.Value);

        if (order == null)
        {
            TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không được phân công đơn này.";
            return RedirectToAction(nameof(Index));
        }

        if (order.Status == "Hoàn thành")
        {
            TempData["Error"] = "Đơn này đã được xác nhận trước đó.";
            return RedirectToAction(nameof(Index));
        }

        if (order.PaymentMethod == "COD")
        {
            order.PaymentStatus = "Đã thanh toán";
            order.PaidAt = DateTime.Now;
        }

        order.Status = "Hoàn thành";

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xác nhận giao thành công đơn #{order.OrderId}.";
        return RedirectToAction(nameof(Index));
    }
}