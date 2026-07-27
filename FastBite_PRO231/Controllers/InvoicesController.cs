using System;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

public class InvoicesController : Controller
{
    private readonly FastBiteDbContext _context;

    private static readonly string[] ValidPaymentMethods =
    {
        "Cash",
        "Banking",
        "Momo"
    };

    public InvoicesController(FastBiteDbContext context)
    {
        _context = context;
    }

    // KIỂM TRA QUYỀN ADMIN HOẶC EMPLOYEE
    private bool CanManageInvoices()
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
            "Bạn không có quyền truy cập chức năng hóa đơn.";

        return RedirectToAction(
            "Login",
            "Login");
    }

    private static bool IsCompletedStatus(string? status)
    {
        return string.Equals(
                   status,
                   "Hoàn thành",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   status,
                   "Completed",
                   StringComparison.OrdinalIgnoreCase);
    }

    private async Task<int?> GetCurrentEmployeeIdAsync()
    {
        var role = HttpContext.Session.GetString("Role");
        var userId = HttpContext.Session.GetInt32("UserId");

        var isEmployee =
            string.Equals(
                role,
                "Employee",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                role,
                "Empolyee",
                StringComparison.OrdinalIgnoreCase);

        if (!isEmployee || !userId.HasValue)
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

    // TẠO LẠI MODEL CHO TRANG CREATE
    private async Task<InvoiceCreateViewModel?>
        BuildCreateViewModelAsync(
            int orderId,
            int? selectedEmployeeId = null,
            string paymentMethod = "Cash")
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(item => item.Customer)
                .ThenInclude(customer => customer.User)
            .Include(item => item.OrderDetails)
                .ThenInclude(detail => detail.Product)
            .FirstOrDefaultAsync(item =>
                item.OrderId == orderId);

        if (order == null)
        {
            return null;
        }

        var employeeId =
            await GetCurrentEmployeeIdAsync();

        if (employeeId.HasValue)
        {
            selectedEmployeeId = employeeId.Value;
        }
        else if (!selectedEmployeeId.HasValue &&
                 order.EmployeeId.HasValue)
        {
            selectedEmployeeId = order.EmployeeId.Value;
        }

        var employees = await _context.Employees
            .AsNoTracking()
            .Include(employee => employee.User)
            .Where(employee => employee.Status == "Đang làm việc")
            .OrderBy(employee => employee.User.FullName)
            .Select(employee =>
                new InvoiceEmployeeOptionViewModel
                {
                    EmployeeId = employee.EmployeeId, 
                    FullName = employee.User.FullName
                })
            .ToListAsync();

        var items = order.OrderDetails
            .Select(detail =>
                new InvoiceLineViewModel
                {
                    ProductId = detail.ProductId, 
                    ProductName = detail.Product == null
                            ? "Sản phẩm"
                            : detail.Product.ProductName,

                    Quantity = detail.Quantity, 
                    UnitPrice = detail.UnitPrice, 
                    SubTotal = detail.Quantity * detail.UnitPrice
                })
            .ToList();

        return new InvoiceCreateViewModel
        {
            OrderId = order.OrderId, 
            EmployeeId = selectedEmployeeId, 
            PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod)
                    ? "Cash"
                    : paymentMethod,

            CustomerName = order.Customer.User.FullName, 
            Phone = order.Customer.User.Phone, 
            Address = order.Customer.Address, 
            OrderDate = order.OrderDate, 
            TotalAmount = order.TotalAmount, 
            Employees = employees, 
            Items = items
        };
    }

    // DANH SÁCH HÓA ĐƠN
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status)
    {
        if (!CanManageInvoices())
        {
            return RedirectToLogin();
        }

        search = search?.Trim() ?? "";
        status = status?.Trim() ?? "";

        var query = _context.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Order)
                .ThenInclude(order => order.Customer)
                    .ThenInclude(customer => customer.User)
            .Include(invoice => invoice.Employee)
                .ThenInclude(employee => employee.User)
            .AsQueryable();

        if (status == "active")
        {
            query = query.Where(invoice =>
                invoice.Status);
        }
        else if (status == "cancelled")
        {
            query = query.Where(invoice =>
                !invoice.Status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (int.TryParse(
                    search,
                    out var number))
            {
                query = query.Where(invoice =>
                    invoice.InvoiceId == number ||
                    invoice.OrderId == number);
            }
            else
            {
                query = query.Where(invoice =>
                    invoice.Order.Customer.User
                        .FullName.Contains(search) ||
                    invoice.Order.Customer.User
                        .UserName.Contains(search) ||
                    invoice.Order.Customer.User
                        .Phone.Contains(search) ||
                    invoice.Employee.User
                        .FullName.Contains(search));
            }
        }

        var invoiceEntities = await query
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .ThenByDescending(invoice => invoice.InvoiceId)
            .ToListAsync();

        var model =
            new InvoiceManagementIndexViewModel
            {
                Search = search,
                StatusFilter = status, 
                TotalInvoices =
                    await _context.Invoices
                        .CountAsync(),

                ActiveInvoices =
                    await _context.Invoices
                        .CountAsync(invoice => invoice.Status),

                CancelledInvoices =
                    await _context.Invoices
                        .CountAsync(invoice => !invoice.Status),

                TotalRevenue =
                    await _context.Invoices
                        .Where(invoice => invoice.Status)
                        .SumAsync(invoice => (decimal?)invoice.TotalAmount)
                    ?? 0m,

                Invoices = invoiceEntities
                    .Select(invoice =>
                        new InvoiceManagementListItemViewModel
                        {
                            InvoiceId = invoice.InvoiceId, 
                            OrderId = invoice.OrderId, 
                            CustomerName = invoice.Order.Customer .User.FullName, 
                            EmployeeName = invoice.Employee.User .FullName, 
                            InvoiceDate = invoice.InvoiceDate, 
                            TotalAmount = invoice.TotalAmount, 
                            PaymentMethod = invoice.PaymentMethod, 
                            Status = invoice.Status
                        })
                    .ToList()
            };

        return View(
            "~/Views/Admin/Invoice/Index.cshtml",
            model);
    }

    // CHI TIẾT HÓA ĐƠN
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!CanManageInvoices())
        {
            return RedirectToLogin();
        }

        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(item => item.Order)
                .ThenInclude(order => order.Customer)
                    .ThenInclude(customer => customer.User)
            .Include(item => item.Employee)
                .ThenInclude(employee => employee.User)
            .Include(item => item.InvoiceDetails)
                .ThenInclude(detail => detail.Product)
            .FirstOrDefaultAsync(item =>
                item.InvoiceId == id);

        if (invoice == null)
        {
            return NotFound();
        }

        var items = invoice.InvoiceDetails
            .Select(detail =>
                new InvoiceLineViewModel
                {
                    ProductId = detail.ProductId,
                    ProductName = detail.Product == null
                            ? "Sản phẩm"
                            : detail.Product.ProductName,

                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    SubTotal = detail.SubTotal
                })
            .ToList();

        var model =
            new InvoiceDetailsViewModel
            {
                InvoiceId = invoice.InvoiceId,

                OrderId = invoice.OrderId,

                CustomerName = invoice.Order.Customer.User.FullName,
                Phone = invoice.Order.Customer.User.Phone,
                Email = invoice.Order.Customer.User.Email,
                Address = invoice.Order.Customer.Address,
                EmployeeName = invoice.Employee.User.FullName,
                InvoiceDate = invoice.InvoiceDate,
                TotalAmount = invoice.TotalAmount,
                PaymentMethod = invoice.PaymentMethod,
                Status = invoice.Status,
                TotalQuantity =
                    items.Sum(item =>
                        item.Quantity),

                Items =
                    items
            };

        return View(
            "~/Views/Admin/Invoice/Details.cshtml",
            model);
    }

    // MỞ FORM TẠO HÓA ĐƠN TỪ ĐƠN HÀNG
    [HttpGet]
    public async Task<IActionResult> Create(int orderId)
    {
        if (!CanManageInvoices())
        {
            return RedirectToLogin();
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Include(item => item.Invoices)
            .FirstOrDefaultAsync(item =>
                item.OrderId == orderId);

        if (order == null)
        {
            TempData["Error"] = "Không tìm thấy đơn hàng.";

            return RedirectToAction(
                "Index",
                "Orders");
        }

        if (!IsCompletedStatus(order.Status))
        {
            TempData["Error"] = "Chỉ có thể tạo hóa đơn cho đơn hàng đã hoàn thành.";

            return RedirectToAction(
                "Details",
                "Orders",
                new { id = orderId });
        }

        if (order.Invoices.Any())
        {
            TempData["Error"] = "Đơn hàng này đã có hóa đơn.";

            return RedirectToAction(
                "Details",
                "Orders",
                new { id = orderId });
        }

        var model =
            await BuildCreateViewModelAsync(orderId);

        if (model == null)
        {
            return NotFound();
        }

        return View(
            "~/Views/Admin/Invoice/Create.cshtml",
            model);
    }

    // LƯU HÓA ĐƠN
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        InvoiceCreateViewModel model)
    {
        if (!CanManageInvoices())
        {
            return RedirectToLogin();
        }

        model.PaymentMethod =
            model.PaymentMethod?.Trim()
            ?? "";

        if (!ValidPaymentMethods.Contains(
                model.PaymentMethod,
                StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.PaymentMethod),
                "Phương thức thanh toán không hợp lệ.");
        }

        var currentEmployeeId =
            await GetCurrentEmployeeIdAsync();

        if (currentEmployeeId.HasValue)
        {
            model.EmployeeId =
                currentEmployeeId.Value;
        }

        if (!model.EmployeeId.HasValue ||
            model.EmployeeId.Value <= 0)
        {
            ModelState.AddModelError(
                nameof(model.EmployeeId),
                "Vui lòng chọn nhân viên lập hóa đơn.");
        }

        var order = await _context.Orders
            .Include(item => item.OrderDetails)
            .Include(item => item.Invoices)
            .FirstOrDefaultAsync(item =>
                item.OrderId == model.OrderId);

        if (order == null)
        {
            ModelState.AddModelError(
                nameof(model.OrderId),
                "Đơn hàng không tồn tại.");
        }
        else
        {
            if (!IsCompletedStatus(order.Status))
            {
                ModelState.AddModelError(
                    nameof(model.OrderId),
                    "Đơn hàng chưa hoàn thành.");
            }

            if (order.Invoices.Any())
            {
                ModelState.AddModelError(
                    nameof(model.OrderId),
                    "Đơn hàng này đã có hóa đơn.");
            }

            if (order.OrderDetails.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.OrderId),
                    "Đơn hàng không có sản phẩm.");
            }
        }

        if (model.EmployeeId.HasValue)
        {
            var employeeExists =
                await _context.Employees
                    .AnyAsync(employee =>
                        employee.EmployeeId ==
                            model.EmployeeId.Value &&
                        employee.Status ==
                            "Đang làm việc");

            if (!employeeExists)
            {
                ModelState.AddModelError(
                    nameof(model.EmployeeId),
                    "Nhân viên không tồn tại hoặc đã nghỉ việc.");
            }
        }

        if (!ModelState.IsValid ||
            order == null ||
            !model.EmployeeId.HasValue)
        {
            var reloadModel =
                await BuildCreateViewModelAsync(
                    model.OrderId,
                    model.EmployeeId,
                    model.PaymentMethod);

            if (reloadModel == null)
            {
                return NotFound();
            }

            return View(
                "~/Views/Admin/Invoice/Create.cshtml",
                reloadModel);
        }

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            var invoice = new Invoice
            {
                OrderId = order.OrderId, 
                EmployeeId = model.EmployeeId.Value, 
                InvoiceDate = DateTime.Now,
                TotalAmount = order.TotalAmount,
                PaymentMethod = model.PaymentMethod,
                Status = true
            };

            foreach (var orderDetail
                     in order.OrderDetails)
            {
                invoice.InvoiceDetails.Add(
                    new InvoiceDetail
                    {
                        ProductId = orderDetail.ProductId, 
                        Quantity = orderDetail.Quantity, 
                        UnitPrice = orderDetail.UnitPrice, 
                        SubTotal = orderDetail.Quantity * orderDetail.UnitPrice
                    });
            }

            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                $"Đã tạo hóa đơn #{invoice.InvoiceId} thành công.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = invoice.InvoiceId
                });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                "",
                "Không thể tạo hóa đơn. Vui lòng thử lại.");

            var reloadModel =
                await BuildCreateViewModelAsync(
                    model.OrderId,
                    model.EmployeeId,
                    model.PaymentMethod);

            if (reloadModel == null)
            {
                return NotFound();
            }

            return View(
                "~/Views/Admin/Invoice/Create.cshtml",
                reloadModel);
        }
    }

    // HỦY HÓA ĐƠN
    // Không xóa dữ liệu, chỉ chuyển Status = false.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!CanManageInvoices())
        {
            return RedirectToLogin();
        }

        var invoice =
            await _context.Invoices
                .FirstOrDefaultAsync(item =>
                    item.InvoiceId == id);

        if (invoice == null)
        {
            return NotFound();
        }

        if (!invoice.Status)
        {
            TempData["Error"] = "Hóa đơn này đã bị hủy trước đó.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        invoice.Status = false;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã hủy hóa đơn #{invoice.InvoiceId}.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }
}