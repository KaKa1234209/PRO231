using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers.Admin;

public class ReportController : Controller
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
        "Đang giao"
    };

    private static readonly string[] CompletedStatuses =
    {
        "Hoàn thành",
        "Completed"
    };

    public ReportController(FastBiteDbContext context)
    {
        _context = context;
    }

    // =========================================
    // KIỂM TRA QUYỀN XEM BÁO CÁO
    // =========================================

    private bool CanViewReport()
    {
        var role = HttpContext.Session.GetString("Role");

        return string.Equals(
                   role,
                   "Admin",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   role,
                   "Employee",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   role,
                   "Empolyee",
                   StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectToLogin()
    {
        TempData["Error"] =
            "Bạn không có quyền truy cập báo cáo thống kê.";

        return RedirectToAction(
            "Login",
            "Login");
    }

    private static bool HasStatus(
        string? currentStatus,
        IEnumerable<string> statuses)
    {
        if (string.IsNullOrWhiteSpace(currentStatus))
        {
            return false;
        }

        return statuses.Any(status =>
            string.Equals(
                currentStatus,
                status,
                StringComparison.OrdinalIgnoreCase));
    }

    // =========================================
    // BÁO CÁO THỐNG KÊ
    // GET: /Report
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Index(
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (!CanViewReport())
        {
            return RedirectToLogin();
        }

        var today = DateTime.Today;

        var startDate =
            fromDate?.Date
            ?? new DateTime(
                today.Year,
                today.Month,
                1);

        var endDate =
            toDate?.Date
            ?? today;

        // Nếu người dùng nhập ngày bắt đầu lớn hơn ngày kết thúc
        // thì tự động đổi lại cho đúng.
        if (startDate > endDate)
        {
            (startDate, endDate) =
                (endDate, startDate);
        }

        // Lấy đến hết ngày kết thúc.
        var endDateExclusive =
            endDate.AddDays(1);

        // =========================================
        // LẤY DANH SÁCH ĐƠN HÀNG
        // =========================================

        var orderEntities = await _context.Orders
            .AsNoTracking()
            .Include(order => order.OrderDetails)
            .Where(order =>
                order.OrderDate >= startDate &&
                order.OrderDate < endDateExclusive)
            .ToListAsync();

        // =========================================
        // LẤY DANH SÁCH HÓA ĐƠN
        // =========================================

        var invoiceEntities = await _context.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.InvoiceDetails)
                .ThenInclude(detail => detail.Product)
                    .ThenInclude(product => product.Category)
            .Include(invoice => invoice.Order)
                .ThenInclude(order => order.Customer)
                    .ThenInclude(customer => customer.User)
            .Where(invoice =>
                invoice.InvoiceDate >= startDate &&
                invoice.InvoiceDate < endDateExclusive)
            .ToListAsync();

        // Chỉ hóa đơn Status = true mới được tính doanh thu.
        var activeInvoices = invoiceEntities
            .Where(invoice => invoice.Status)
            .ToList();

        // =========================================
        // LẤY DỮ LIỆU TỒN KHO
        // =========================================

        var inventoryEntities = await _context.Inventories
            .AsNoTracking()
            .Include(inventory => inventory.Product)
            .OrderBy(inventory => inventory.Quantity)
            .ToListAsync();

        // =========================================
        // THỐNG KÊ ĐƠN HÀNG
        // =========================================

        var totalOrders =
            orderEntities.Count;

        var pendingOrders =
            orderEntities.Count(order =>
                HasStatus(
                    order.Status,
                    PendingStatuses));

        var processingOrders =
            orderEntities.Count(order =>
                HasStatus(
                    order.Status,
                    ProcessingStatuses));

        var completedOrders =
            orderEntities.Count(order =>
                HasStatus(
                    order.Status,
                    CompletedStatuses));

        var cancelledOrders =
            orderEntities.Count(order =>
                string.Equals(
                    order.Status,
                    "Đã hủy",
                    StringComparison.OrdinalIgnoreCase));

        // =========================================
        // THỐNG KÊ HÓA ĐƠN VÀ DOANH THU
        // =========================================

        var totalInvoices =
            activeInvoices.Count;

        var totalRevenue =
            activeInvoices.Sum(invoice =>
                invoice.TotalAmount);

        var averageOrderValue =
            totalInvoices > 0
                ? totalRevenue / totalInvoices
                : 0m;

        var totalProductsSold =
            activeInvoices
                .SelectMany(invoice =>
                    invoice.InvoiceDetails)
                .Sum(detail =>
                    detail.Quantity);

        // =========================================
        // DOANH THU THEO NGÀY
        // =========================================

        var dailyRevenueRaw =
            activeInvoices
                .GroupBy(invoice =>
                    invoice.InvoiceDate.Date)
                .Select(group =>
                    new
                    {
                        Date = group.Key,

                        InvoiceCount =
                            group.Count(),

                        Revenue =
                            group.Sum(invoice =>
                                invoice.TotalAmount)
                    })
                .OrderBy(item =>
                    item.Date)
                .ToList();

        var maximumDailyRevenue =
            dailyRevenueRaw.Count == 0
                ? 0m
                : dailyRevenueRaw.Max(item =>
                    item.Revenue);

        var dailyRevenue =
            dailyRevenueRaw
                .Select(item =>
                    new ReportDailyRevenueViewModel
                    {
                        Date =
                            item.Date,

                        InvoiceCount =
                            item.InvoiceCount,

                        Revenue =
                            item.Revenue,

                        Percent =
                            maximumDailyRevenue <= 0
                                ? 0
                                : Math.Round(
                                    item.Revenue /
                                    maximumDailyRevenue *
                                    100m,
                                    2)
                    })
                .ToList();

        // =========================================
        // SẢN PHẨM BÁN CHẠY
        // =========================================

        var invoiceDetailEntities =
            activeInvoices
                .SelectMany(invoice =>
                    invoice.InvoiceDetails)
                .ToList();

        var topProductsRaw =
            invoiceDetailEntities
                .GroupBy(detail =>
                    new
                    {
                        detail.ProductId,

                        ProductName =
                            detail.Product?.ProductName
                            ?? "Sản phẩm",

                        CategoryName =
                            detail.Product?.Category?.CategoryName
                            ?? "Chưa phân loại"
                    })
                .Select(group =>
                    new
                    {
                        group.Key.ProductId,
                        group.Key.ProductName,
                        group.Key.CategoryName,

                        QuantitySold =
                            group.Sum(detail =>
                                detail.Quantity),

                        Revenue =
                            group.Sum(detail =>
                                detail.SubTotal > 0
                                    ? detail.SubTotal
                                    : detail.UnitPrice *
                                      detail.Quantity)
                    })
                .OrderByDescending(item =>
                    item.QuantitySold)
                .ThenByDescending(item =>
                    item.Revenue)
                .Take(10)
                .ToList();

        var maximumProductQuantity =
            topProductsRaw.Count == 0
                ? 0
                : topProductsRaw.Max(item =>
                    item.QuantitySold);

        var topProducts =
            topProductsRaw
                .Select(item =>
                    new ReportTopSellingProductViewModel
                    {
                        ProductId =
                            item.ProductId,

                        ProductName =
                            item.ProductName,

                        CategoryName =
                            item.CategoryName,

                        QuantitySold =
                            item.QuantitySold,

                        Revenue =
                            item.Revenue,

                        Percent =
                            maximumProductQuantity <= 0
                                ? 0
                                : Math.Round(
                                    (decimal)item.QuantitySold /
                                    maximumProductQuantity *
                                    100m,
                                    2)
                    })
                .ToList();

        // =========================================
        // KHÁCH HÀNG CHI TIÊU NHIỀU
        // =========================================

        var topCustomers =
            activeInvoices
                .GroupBy(invoice =>
                    new
                    {
                        CustomerId = invoice.Order.CustomerId,
                        FullName = invoice.Order.Customer.User.FullName,
                        Phone = invoice.Order.Customer.User.Phone
                    })
                .Select(group =>
                    new ReportTopCustomerViewModel
                    {
                        CustomerId =
                            group.Key.CustomerId,

                        FullName =
                            group.Key.FullName
                            ?? "Khách hàng",

                        Phone =
                            group.Key.Phone
                            ?? "",

                        OrderCount =
                            group.Select(invoice =>
                                    invoice.OrderId)
                                .Distinct()
                                .Count(),

                        TotalSpent =
                            group.Sum(invoice =>
                                invoice.TotalAmount)
                    })
                .OrderByDescending(customer =>
                    customer.TotalSpent)
                .ThenByDescending(customer =>
                    customer.OrderCount)
                .Take(10)
                .ToList();

        // =========================================
        // CẢNH BÁO TỒN KHO
        // =========================================

        var lowStockProducts =
            inventoryEntities.Count(inventory =>
                inventory.Quantity > 0 &&
                inventory.Quantity <= 10);

        var outOfStockProducts =
            inventoryEntities.Count(inventory =>
                inventory.Quantity <= 0);

        var stockWarnings =
            inventoryEntities
                .Where(inventory =>
                    inventory.Quantity <= 10)
                .Take(10)
                .Select(inventory =>
                    new ReportLowStockViewModel
                    {
                        InventoryId =
                            inventory.InventoryId,

                        ProductId =
                            inventory.ProductId,

                        ProductName =
                            inventory.Product?.ProductName
                            ?? "Sản phẩm",

                        Quantity =
                            inventory.Quantity,

                        Unit =
                            inventory.Unit
                            ?? "Phần",

                        StockStatus =
                            inventory.Quantity <= 0
                                ? "Hết hàng"
                                : "Sắp hết"
                    })
                .ToList();

        // =========================================
        // TẠO MODEL TRẢ VỀ VIEW
        // =========================================

        var model = new ReportViewModel
        {
            FromDate =
                startDate,

            ToDate =
                endDate,

            TotalOrders =
                totalOrders,

            PendingOrders =
                pendingOrders,

            ProcessingOrders =
                processingOrders,

            CompletedOrders =
                completedOrders,

            CancelledOrders =
                cancelledOrders,

            TotalInvoices =
                totalInvoices,

            TotalRevenue =
                totalRevenue,

            AverageOrderValue =
                averageOrderValue,

            TotalProductsSold =
                totalProductsSold,

            LowStockProducts =
                lowStockProducts,

            OutOfStockProducts =
                outOfStockProducts,

            DailyRevenue =
                dailyRevenue,

            TopProducts =
                topProducts,

            TopCustomers =
                topCustomers,

            StockWarnings =
                stockWarnings
        };

        return View(
            "~/Views/Admin/Report/Index.cshtml",
            model);
    }
}