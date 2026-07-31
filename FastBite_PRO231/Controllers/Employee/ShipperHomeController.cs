using FastBite_PRO231.Common;
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

        var allMyOrders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Where(o => o.ShipperId == shipperId.Value)
            .ToListAsync();

        // IsActiveOrder chạy trên List<Order> đã load về (LINQ-to-Objects) nên dùng được ở đây
        var myActiveOrders = allMyOrders
            .Where(o => OrderStatusConstants.IsActiveOrder(o.Status))
            .OrderBy(o => o.OrderDate)
            .Select(o => new ShipperOrderViewModel
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                CustomerName = o.Customer.User!.FullName ?? o.Customer.User.UserName ?? "Khách hàng",
                CustomerPhone = o.Customer.User!.Phone ?? "",
                DeliveryAddress = o.DeliveryAddress ?? "",
                Latitude = o.Latitude,
                Longitude = o.Longitude,
                Note = o.Note,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                Status = o.Status
            })
            .ToList();

        // Đây là query EF Core (chưa ToListAsync) nên không gọi được IsActiveOrder,
        // phải so sánh trực tiếp với hằng số để EF dịch được sang SQL.
        var availableOrders = _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Where(o =>
                o.ShipperId == null &&
                o.Status != OrderStatusConstants.Completed &&
                o.Status != OrderStatusConstants.Cancelled)
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
                o.Status == OrderStatusConstants.Completed &&
                o.PaidAt.HasValue &&
                o.PaidAt.Value.Date == today),

            PendingSettlementAmount = allMyOrders
                .Where(o =>
                    o.PaymentMethod == OrderStatusConstants.PaymentMethodCod &&
                    o.PaymentStatus == OrderStatusConstants.PaymentStatusPaid &&
                    o.SettlementStatus == OrderStatusConstants.SettlementPending)
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
                o.Status != OrderStatusConstants.Completed &&
                o.Status != OrderStatusConstants.Cancelled)
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

        if (order.Status == OrderStatusConstants.Completed)
        {
            TempData["Error"] = "Đơn này đã được xác nhận trước đó.";
            return RedirectToAction(nameof(Index));
        }

        if (order.PaymentMethod == OrderStatusConstants.PaymentMethodCod)
        {
            order.PaymentStatus = OrderStatusConstants.PaymentStatusPaid;
            order.PaidAt = DateTime.Now;
        }

        order.Status = OrderStatusConstants.Completed;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xác nhận giao thành công đơn #{order.OrderId}.";
        return RedirectToAction(nameof(Index));
    }
}