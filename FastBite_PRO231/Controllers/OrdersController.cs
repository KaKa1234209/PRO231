using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Common;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

public class OrdersController : Controller
{
    private readonly FastBiteDbContext _context;

    public OrdersController(FastBiteDbContext context)
    {
        _context = context;
    }

    private bool CanManageOrders()
    {
        var role = HttpContext.Session.GetString("Role");
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectToLogin()
    {
        TempData["Error"] =
            "Bạn không có quyền truy cập chức năng quản lý đơn hàng.";

        return RedirectToAction(
            "Login",
            "Login");
    }

    private async Task<int?> GetCurrentEmployeeIdAsync()
    {
        var role = HttpContext.Session.GetString("Role");
        var userId = HttpContext.Session.GetInt32("UserId");

        if (!string.Equals(
                role,
                "Employee",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!userId.HasValue)
        {
            return null;
        }

        return await _context.Employees
            .Where(employee =>
                employee.UserId == userId.Value)
            .Select(employee =>
                (int?)employee.EmployeeId)
            .FirstOrDefaultAsync();
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
                StringComparison.OrdinalIgnoreCase))
        {
            return image;
        }

        if (image.StartsWith(
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

    // Trang chủ: Danh sách + tìm kiếm
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status)
    {
        if (!CanManageOrders())
        {
            return RedirectToLogin();
        }

        search = search?.Trim() ?? "";
        status = status?.Trim() ?? "";

        var query = _context.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
                .ThenInclude(customer => customer.User)
            .Include(order => order.Employee)
                .ThenInclude(employee => employee.User)
            .Include(order => order.OrderDetails)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(order =>
                order.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (int.TryParse(search, out var orderId))
            {
                query = query.Where(order =>
                    order.OrderId == orderId);
            }
            else
            {
                query = query.Where(order =>
                    order.Customer.User.FullName.Contains(search) ||
                    order.Customer.User.UserName.Contains(search) ||
                    order.Customer.User.Phone.Contains(search));
            }
        }

        var orderEntities = await query
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.OrderId)
            .ToListAsync();

        var model = new OrderManagementIndexViewModel
        {
            Search = search,
            StatusFilter = status,

            TotalOrders =
                await _context.Orders.CountAsync(),

            PendingOrders = await _context.Orders.CountAsync(o =>
                OrderStatusConstants.PendingStatuses.Contains(o.Status)),

            ProcessingOrders = await _context.Orders.CountAsync(o =>
                OrderStatusConstants.ProcessingStatuses.Contains(o.Status)),

            CompletedOrders = await _context.Orders.CountAsync(o =>
                o.Status == OrderStatusConstants.Completed),

            Orders = orderEntities
                .Select(order =>
                    new OrderManagementListItemViewModel
                    {
                        OrderId = order.OrderId, 
                        CustomerName = order.Customer.User.FullName, 
                        Phone = order.Customer.User.Phone, 
                        EmployeeName = order.Employee == null
                                ? "Chưa tiếp nhận"
                                : order.Employee.User.FullName, 
                        OrderDate = order.OrderDate,
                        Status = order.Status, 
                        TotalQuantity = order.OrderDetails.Sum(detail =>
                                detail.Quantity), 
                        TotalAmount = order.TotalAmount
                    })
                .ToList()
        };

        return View(
            "~/Views/Admin/Order/Index.cshtml",
            model);
    }

    // ==========================================
    // CHI TIẾT ĐƠN
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!CanManageOrders())
        {
            return RedirectToLogin();
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Include(item => item.Customer)
                .ThenInclude(customer => customer.User)
            .Include(item => item.Employee)
                .ThenInclude(employee => employee.User)
            .Include(item => item.OrderDetails)
                .ThenInclude(detail => detail.Product)
            .Include(item => item.Invoices)
            .FirstOrDefaultAsync(item =>
                item.OrderId == id);

        if (order == null)
        {
            return NotFound();
        }

        var items = order.OrderDetails
            .Select(detail =>
                new OrderManagementDetailItemViewModel
                {
                    ProductId = detail.ProductId,

                    ProductName =
                        detail.Product?.ProductName
                        ?? "Sản phẩm",

                    ImageUrl =
                        NormalizeImageUrl(
                            detail.Product?.Image),

                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,

                    SubTotal =
                        detail.UnitPrice *
                        detail.Quantity
                })
            .ToList();

        var model = new OrderManagementDetailsViewModel
        {
            OrderId = order.OrderId,

            CustomerName = order.Customer.User.FullName,
            UserName = order.Customer.User.UserName,
            Phone = order.Customer.User.Phone,
            Email = order.Customer.User.Email,

            Address = order.DeliveryAddress ?? order.Customer.Address,

            Latitude = order.Latitude,
            Longitude = order.Longitude,

            EmployeeName = order.Employee == null
        ? "Chưa tiếp nhận"
        : order.Employee.User.FullName,

            OrderDate = order.OrderDate,
            Status = order.Status,

            TotalQuantity = items.Sum(item => item.Quantity),
            TotalAmount = order.TotalAmount,

            HasInvoice = order.Invoices.Any(),

            Items = items
        };

        return View(
            "~/Views/Admin/Order/Details.cshtml",
            model);
    }

    // ==========================================
    // CẬP NHẬT TRẠNG THÁI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        string status)
    {
        var allowedStatuses = new HashSet<string>(
            OrderStatusConstants.AllowedOrderStatuses,
            StringComparer.OrdinalIgnoreCase);

        if (!CanManageOrders())
        {
            return RedirectToLogin();
        }

        status = status?.Trim() ?? "";

        if (!allowedStatuses.Contains(status))
        {
            TempData["Error"] = "Trạng thái đơn hàng không hợp lệ.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var order = await _context.Orders
            .Include(item => item.OrderDetails)
            .Include(item => item.Invoices)
            .FirstOrDefaultAsync(item =>
                item.OrderId == id);

        if (order == null)
        {
            return NotFound();
        }

        if (string.Equals(order.Status, OrderStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(status, OrderStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Đơn đã hủy không thể kích hoạt lại.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
        }

        if (string.Equals(order.Status, OrderStatusConstants.Completed, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(status, OrderStatusConstants.Completed, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Đơn đã hoàn thành không thể đổi lại trạng thái.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
        }

        var isCancelling =
            !string.Equals(order.Status, OrderStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(status, OrderStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase);


        if (isCancelling && order.Invoices.Any())
        {
            TempData["Error"] = "Đơn đã có hóa đơn nên không thể hủy.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            if (isCancelling)
            {
                foreach (var detail in order.OrderDetails)
                {
                    var inventory =
                        await _context.Inventories
                            .FirstOrDefaultAsync(item =>
                                item.ProductId ==
                                detail.ProductId);

                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            ProductId = detail.ProductId, 
                            Quantity = detail.Quantity, 
                            Unit = "Phần",
                            UpdateAt = DateTime.Now
                        };

                        _context.Inventories.Add(
                            inventory);
                    }
                    else
                    {
                        inventory.Quantity += detail.Quantity;
                        inventory.UpdateAt = DateTime.Now;
                    }
                }
            }

            var employeeId =
                await GetCurrentEmployeeIdAsync();

            if (employeeId.HasValue)
            {
                if (!order.EmployeeId.HasValue)
                {
                    order.EmployeeId = employeeId.Value;
                }
            }

            order.Status = status;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                $"Đã cập nhật đơn hàng #{order.OrderId} " +
                $"sang trạng thái “{status}”.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            TempData["Error"] =
                "Không thể cập nhật đơn hàng. Vui lòng thử lại.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TakeOrder(int orderId)
    {
        var role = HttpContext.Session.GetString("Role");
        var userId = HttpContext.Session.GetInt32("UserId");

        if (!string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase) || userId == null)
        {
            return RedirectToLogin();
        }

        var order = await _context.Orders.FindAsync(orderId);

        if (order == null)
        {
            return NotFound();
        }

        if (!string.Equals(order.Status, OrderStatusConstants.Pending, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Không thể nhận đơn này.";
            return RedirectToAction(nameof(Index));
        }

        if (order.EmployeeId != null)
        {
            TempData["Error"] = "Đơn hàng đã được nhận.";
            return RedirectToAction(nameof(Index));
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(item => item.UserId == userId.Value);

        if (employee == null)
        {
            return BadRequest("Tài khoản này không phải nhân viên.");
        }

        order.EmployeeId = employee.EmployeeId;
        order.Status = OrderStatusConstants.Processing;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Bạn đã nhận đơn hàng #{order.OrderId}.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignEmployee(int orderId, int employeeId)
    {
        var role = HttpContext.Session.GetString("Role");

        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToLogin();
        }

        var order = await _context.Orders.FindAsync(orderId);

        if (order == null)
        {
            return NotFound();
        }

        if (!string.Equals(order.Status, OrderStatusConstants.Pending, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Không thể phân công đơn này.";
            return RedirectToAction(nameof(Index));
        }

        if (order.EmployeeId != null)
        {
            TempData["Error"] = "Đơn hàng đã có nhân viên xử lý.";
            return RedirectToAction(nameof(Index));
        }

        var employeeExists = await _context.Employees.AnyAsync(item => item.EmployeeId == employeeId);

        if (!employeeExists)
        {
            TempData["Error"] = "Nhân viên không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        order.EmployeeId = employeeId;
        order.Status = OrderStatusConstants.Processing;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã phân công đơn hàng #{order.OrderId}.";

        return RedirectToAction(nameof(Index));
    }
}