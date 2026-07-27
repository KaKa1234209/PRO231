using System;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

public class EmployeeHomeController : Controller
{
    private readonly FastBiteDbContext _context;

    private static readonly string[] PendingStatuses =
    {
        "Đang chờ xử lý",
        "Chờ xử lý",
        "Chờ xác nhận"
    };

    private static readonly string[] ProcessingStatuses =
    {
        "Đang xử lý",
        "Đang chuẩn bị",
        "Đang giao",
        "Processing"
    };

    private static readonly string[] CompletedStatuses =
    {
        "Hoàn thành",
        "Completed"
    };

    public EmployeeHomeController(
        FastBiteDbContext context)
    {
        _context = context;
    }

    private bool IsEmployee()
    {
        var role =
            HttpContext.Session.GetString("Role");

        return string.Equals(
                   role,
                   "Employee",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   role,
                   "Empolyee",
                   StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectUnauthorized()
    {
        TempData["Error"] =
            "Vui lòng đăng nhập bằng tài khoản nhân viên.";

        return RedirectToAction(
            "Login",
            "Login");
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsEmployee())
        {
            return RedirectUnauthorized();
        }

        var userId =
            HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue)
        {
            return RedirectUnauthorized();
        }

        var employee =
            await _context.Employees
                .AsNoTracking()
                .Include(item => item.User)
                .FirstOrDefaultAsync(item =>
                    item.UserId == userId.Value);

        if (employee == null)
        {
            HttpContext.Session.Clear();

            TempData["Error"] =
                "Không tìm thấy hồ sơ nhân viên.";

            return RedirectToAction(
                "Login",
                "Login");
        }

        if (!string.Equals(
                employee.Status,
                "Đang làm việc",
                StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.Clear();

            TempData["Error"] =
                "Tài khoản nhân viên đã ngừng hoạt động.";

            return RedirectToAction(
                "Login",
                "Login");
        }

        var today =
            DateTime.Today;

        var tomorrow =
            today.AddDays(1);

        var employeeId =
            employee.EmployeeId;

        // =====================================
        // THỐNG KÊ ĐƠN HÀNG
        // =====================================

        var pendingStoreOrders =
            await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    order.EmployeeId == null &&
                    PendingStatuses.Contains(
                        order.Status));

        var assignedOrders =
            await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    order.EmployeeId == employeeId);

        var todayAssignedOrders =
            await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    order.EmployeeId == employeeId &&
                    order.OrderDate >= today &&
                    order.OrderDate < tomorrow);

        var processingOrders =
            await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    order.EmployeeId == employeeId &&
                    ProcessingStatuses.Contains(
                        order.Status));

        var completedOrders =
            await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    order.EmployeeId == employeeId &&
                    CompletedStatuses.Contains(
                        order.Status));

        // =====================================
        // THỐNG KÊ HÓA ĐƠN
        // =====================================

        var totalInvoices =
            await _context.Invoices
                .AsNoTracking()
                .CountAsync(invoice =>
                    invoice.EmployeeId == employeeId);

        var invoiceRevenue =
            await _context.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.EmployeeId == employeeId &&
                    invoice.Status)
                .SumAsync(invoice =>
                    (decimal?)invoice.TotalAmount)
            ?? 0m;

        // =====================================
        // HÀNG ĐỢI CÔNG VIỆC
        // Gồm đơn của nhân viên và đơn chưa phân công.
        // =====================================

        var workQueue =
            await _context.Orders
                .AsNoTracking()
                .Include(order => order.Customer)
                    .ThenInclude(customer =>
                        customer.User)
                .Include(order => order.Invoices)
                .Where(order =>
                    order.EmployeeId == employeeId ||
                    (
                        order.EmployeeId == null &&
                        PendingStatuses.Contains(
                            order.Status)
                    ))
                .OrderBy(order =>
                    order.EmployeeId == null ? 0 : 1)
                .ThenByDescending(order =>
                    order.OrderDate)
                .Take(12)
                .Select(order =>
                    new EmployeeDashboardOrderItemViewModel
                    {
                        OrderId =
                            order.OrderId,

                        CustomerName =
                            order.Customer.User.FullName,

                        OrderDate =
                            order.OrderDate,

                        TotalAmount =
                            order.TotalAmount,

                        Status =
                            order.Status,

                        IsAssignedToMe =
                            order.EmployeeId == employeeId,

                        IsUnassigned =
                            order.EmployeeId == null,

                        HasInvoice =
                            order.Invoices.Any()
                    })
                .ToListAsync();

        // =====================================
        // HÓA ĐƠN GẦN ĐÂY CỦA NHÂN VIÊN
        // =====================================

        var recentInvoices =
            await _context.Invoices
                .AsNoTracking()
                .Include(invoice => invoice.Order)
                    .ThenInclude(order =>
                        order.Customer)
                        .ThenInclude(customer =>
                            customer.User)
                .Where(invoice =>
                    invoice.EmployeeId == employeeId)
                .OrderByDescending(invoice =>
                    invoice.InvoiceDate)
                .Take(8)
                .Select(invoice =>
                    new EmployeeDashboardInvoiceItemViewModel
                    {
                        InvoiceId =
                            invoice.InvoiceId,

                        OrderId =
                            invoice.OrderId,

                        CustomerName =
                            invoice.Order.Customer.User
                                .FullName,

                        InvoiceDate =
                            invoice.InvoiceDate,

                        TotalAmount =
                            invoice.TotalAmount,

                        PaymentMethod =
                            invoice.PaymentMethod,

                        Status =
                            invoice.Status
                    })
                .ToListAsync();

        // =====================================
        // CẢNH BÁO TỒN KHO
        // =====================================

        var stockWarnings =
            await _context.Inventories
                .AsNoTracking()
                .Include(inventory =>
                    inventory.Product)
                .Where(inventory =>
                    inventory.Quantity <= 10)
                .OrderBy(inventory =>
                    inventory.Quantity)
                .ThenBy(inventory =>
                    inventory.Product.ProductName)
                .Take(8)
                .Select(inventory =>
                    new EmployeeDashboardStockItemViewModel
                    {
                        InventoryId =
                            inventory.InventoryId,

                        ProductId =
                            inventory.ProductId,

                        ProductName =
                            inventory.Product.ProductName,

                        Quantity =
                            inventory.Quantity,

                        Unit =
                            inventory.Unit,

                        StatusText =
                            inventory.Quantity <= 0
                                ? "Hết hàng"
                                : "Sắp hết"
                    })
                .ToListAsync();

        var model =
            new EmployeeDashboardViewModel
            {
                EmployeeId =
                    employee.EmployeeId,

                EmployeeName =
                    employee.User.FullName,

                Position =
                    employee.Position,

                HireDate =
                    employee.HireDate,

                PendingStoreOrders =
                    pendingStoreOrders,

                AssignedOrders =
                    assignedOrders,

                TodayAssignedOrders =
                    todayAssignedOrders,

                ProcessingOrders =
                    processingOrders,

                CompletedOrders =
                    completedOrders,

                TotalInvoices =
                    totalInvoices,

                InvoiceRevenue =
                    invoiceRevenue,

                WorkQueue =
                    workQueue,

                RecentInvoices =
                    recentInvoices,

                StockWarnings =
                    stockWarnings
            };

        return View(
            "~/Views/EmployeeHome/Index.cshtml",
            model);
    }
}