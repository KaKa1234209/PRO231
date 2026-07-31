using System;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using FastBite_PRO231.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers.Admin;

public class AdminDashboardController : Controller
{
    private readonly FastBiteDbContext _context;

    public AdminDashboardController(
        FastBiteDbContext context)
    {
        _context = context;
    }

    //Kiểm quyền admin ktra theo role lưu trong Session (thường được set lúc đăng nhập).
    private bool IsAdmin()
    {
        var role = HttpContext.Session.GetString("Role");
        //so sánh 2 chuỗi ko phân hoa thg
        return string.Equals(
            role,
            "Admin",
            StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectUnauthorized()
    {
        TempData["Error"] = "Bạn cần đăng nhập bằng tài khoản Admin.";
        return RedirectToAction("Login", "Login");
    }

    [HttpGet]
    [Route("AdminDashboard")]
    [Route("admin/dashboard")]
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var weekStart = today.AddDays(-6);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        //Doanh Thu - Chỉ tính đơn còn hoạt động
        //Theo ngày
        var revenueToday = await _context.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.Status &&
                    invoice.InvoiceDate >= today &&
                    invoice.InvoiceDate < tomorrow)
                .SumAsync(invoice => (decimal?)invoice.TotalAmount) ?? 0m; //nếu null thì để 0

        //Theo tuần
        var revenueWeek = await _context.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.Status &&
                    invoice.InvoiceDate >= weekStart &&
                    invoice.InvoiceDate < tomorrow)
                .SumAsync(invoice => (decimal?)invoice.TotalAmount) ?? 0m;

        //Theo tháng
        var revenueMonth = await _context.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.Status &&
                    invoice.InvoiceDate >= monthStart &&
                    invoice.InvoiceDate < tomorrow)
                .SumAsync(invoice => (decimal?)invoice.TotalAmount) ?? 0m;

        //Thống kê đơn hàng
        var totalOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync();

        var todayOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    order.OrderDate >= today &&
                    order.OrderDate < tomorrow);

        var pendingOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    OrderStatusConstants.PendingStatuses.Contains(
                        order.Status));

        var processingOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    OrderStatusConstants.ProcessingStatuses.Contains(
                        order.Status));

        var completedOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    OrderStatusConstants.CompletedStatuses.Contains(
                        order.Status));

        var cancelledOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == OrderStatusConstants.Cancelled);

        //Thống kê cửa hàng
        var totalCustomers = await _context.Customers
                .AsNoTracking()
                .CountAsync();

        var totalProducts = await _context.Products
                .AsNoTracking()
                .CountAsync();

        var workingEmployees = await _context.Employees
                .AsNoTracking()
                .CountAsync(employee =>
                    employee.Status ==
                    OrderStatusConstants.ShipperWorking);

        var activePromotions = await _context.Promotions
                .AsNoTracking()
                .CountAsync(promotion =>
                    promotion.Status ==
                    "Đang hoạt động");

        //Sp sắp hết hàng
        var lowStockProducts = await _context.Inventories
                .AsNoTracking()
                .CountAsync(inventory =>
                    inventory.Quantity > 0 &&
                    inventory.Quantity <= 10);

        //Sp hết hàng
        var outOfStockProducts = await _context.Inventories
                .AsNoTracking()
                .CountAsync(inventory =>
                    inventory.Quantity <= 0);

        //Doanh thu 7 ngày gần nhất
        var dailyRevenueRaw = await _context.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.Status &&
                    invoice.InvoiceDate >= weekStart &&
                    invoice.InvoiceDate < tomorrow)
                .GroupBy(invoice => invoice.InvoiceDate.Date)
                .Select(group =>
                    new
                    {
                        Date = group.Key,
                        InvoiceCount = group.Count(),
                        Revenue = group.Sum(invoice => invoice.TotalAmount)
                    })
                .OrderBy(item => item.Date)
                .ToListAsync();

        var maximumDailyRevenue =
            dailyRevenueRaw.Count == 0
                ? 0m
                : dailyRevenueRaw.Max(item => item.Revenue); //giá trị cao nhất trg var dailyRevenueRaw

        //Tính phần trăm vẽ biểu đồ
        var dailyRevenue = dailyRevenueRaw
                .Select(item =>
                    new AdminDashboardDailyRevenueViewModel
                    {
                        Date = item.Date,
                        InvoiceCount = item.InvoiceCount,
                        Revenue = item.Revenue,
                        Percent = maximumDailyRevenue <= 0
                                ? 0
                                : Math.Round(
                                    item.Revenue /
                                    maximumDailyRevenue *
                                    100m,
                                    2)
                    })
                .ToList();

        // SẢN PHẨM BÁN CHẠY TRONG THÁNG
        // Chỉ lấy từ hóa đơn còn hoạt động.
        var topProductsRaw =
            await _context.InvoiceDetails
                .AsNoTracking()
                .Where(detail =>
                    detail.Invoice.Status &&
                    detail.Invoice.InvoiceDate >= monthStart &&
                    detail.Invoice.InvoiceDate < tomorrow)
                .GroupBy(detail =>
                    new
                    {
                        detail.ProductId,
                        detail.Product.ProductName
                    })
                .Select(group =>
                    new
                    {
                        ProductId = group.Key.ProductId,
                        ProductName = group.Key.ProductName,
                        Quantity = group.Sum(detail => detail.Quantity),
                        Revenue = group.Sum(detail => detail.SubTotal)
                    })
                .OrderByDescending(item => item.Quantity)
                .ThenByDescending(item => item.Revenue)
                .Take(5)
                .ToListAsync();

        var maximumProductQuantity = topProductsRaw.Count == 0
                ? 0
                : topProductsRaw.Max(item => item.Quantity);

        var topProducts = topProductsRaw
                .Select(item =>
                    new AdminDashboardTopProductViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Revenue = item.Revenue,
                        Percent = maximumProductQuantity <= 0
                                ? 0
                                : Math.Round(
                                    (decimal)item.Quantity /
                                    maximumProductQuantity *
                                    100m,
                                    2)
                    })
                .ToList();

        //Đơn hàng gần đây
        var recentOrders =
            await _context.Orders
                .AsNoTracking()
                .Include(order => order.Customer)
                    .ThenInclude(customer => customer!.User)
                .Include(order => order.Employee)
                    .ThenInclude(employee => employee!.User) //null-forgiving operator: Báo cho compilor bt ee can null nhg t bt nó ko null nên k warning
                .Include(order => order.Invoices)
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.OrderId)
                .Take(8)
                .Select(order =>
                    new AdminDashboardRecentOrderViewModel
                    {
                        OrderId = order.OrderId,
                        // FIX: tránh NullReferenceException nếu Customer hoặc Customer.User bị null
                        // (dữ liệu lỗi / quan hệ không bắt buộc), thay vì giả định luôn tồn tại.
                        CustomerName = order.Customer != null && order.Customer.User != null
                                ? order.Customer.User.FullName
                                : "Khách vãng lai",
                        EmployeeName = order.Employee == null || order.Employee.User == null
                                ? "Chưa phân công"
                                : order.Employee.User.FullName,
                        OrderDate = order.OrderDate,
                        TotalAmount = order.TotalAmount,
                        Status = order.Status,
                        HasInvoice = order.Invoices.Any() //Xuất hóa đơn chx
                    })
                .ToListAsync();

        //Cảnh báo tồn kho
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
                    new AdminDashboardStockWarningViewModel
                    {
                        InventoryId = inventory.InventoryId,
                        ProductId = inventory.ProductId,
                        ProductName = inventory.Product.ProductName,
                        Quantity = inventory.Quantity,
                        Unit = inventory.Unit,
                        StatusText = inventory.Quantity <= 0
                                ? "Hết hàng"
                                : "Sắp hết"
                    })
                .ToListAsync();

        //Tạo model
        var model =
            new AdminDashboardViewModel
            {
                RevenueToday = revenueToday,
                RevenueWeek = revenueWeek,
                RevenueMonth = revenueMonth,
                TotalOrders = totalOrders,
                TodayOrders = todayOrders,
                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                CompletedOrders = completedOrders,
                CancelledOrders = cancelledOrders,
                TotalCustomers = totalCustomers,
                TotalProducts = totalProducts,
                WorkingEmployees = workingEmployees,
                ActivePromotions = activePromotions,
                LowStockProducts = lowStockProducts,
                OutOfStockProducts = outOfStockProducts,
                DailyRevenue = dailyRevenue,
                TopProducts = topProducts,
                RecentOrders = recentOrders,
                StockWarnings = stockWarnings
            };

        return View(
            "~/Views/Admin/Dashboard/Index.cshtml",
            model);
    }
}