using System;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers.Admin;

public class EmployeesController : Controller
{
    private readonly FastBiteDbContext _context;

    private readonly PasswordHasher<User> _passwordHasher =
        new();

    private static readonly string[] ValidEmployeeStatuses =
    {
        "Đang làm việc",
        "Đã nghỉ việc"
    };

    public EmployeesController(FastBiteDbContext context)
    {
        _context = context;
    }

    // =========================================
    // KIỂM TRA QUYỀN ADMIN
    // =========================================

    private bool IsAdmin()
    {
        var role =
            HttpContext.Session.GetString("Role");

        return string.Equals(
            role,
            "Admin",
            StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectUnauthorized()
    {
        var userId =
            HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue)
        {
            TempData["Error"] =
                "Vui lòng đăng nhập tài khoản Admin.";

            return RedirectToAction(
                "Login",
                "Login");
        }

        TempData["Error"] =
            "Chỉ tài khoản Admin mới được quản lý nhân viên.";

        return RedirectToAction(
            "Index",
            "Home");
    }

    private static string GetAccountStatus(
        string employeeStatus)
    {
        return string.Equals(
            employeeStatus,
            "Đang làm việc",
            StringComparison.OrdinalIgnoreCase)
                ? "Hoạt động"
                : "Ngừng hoạt động";
    }

    private static bool IsCompletedOrder(
        string? status)
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

    private void NormalizeForm(
        EmployeeManagementFormViewModel model)
    {
        model.FullName =
            model.FullName?.Trim() ?? "";

        model.UserName =
            model.UserName?.Trim() ?? "";

        model.Email =
            model.Email?.Trim() ?? "";

        model.Phone =
            model.Phone?.Trim() ?? "";

        model.Position =
            model.Position?.Trim() ?? "";

        model.Status =
            model.Status?.Trim() ?? "";

        model.Password =
            model.Password?.Trim() ?? "";

        model.ConfirmPassword =
            model.ConfirmPassword?.Trim() ?? "";
    }

    private async Task ValidateFormAsync(
        EmployeeManagementFormViewModel model,
        int? currentUserId = null,
        bool requirePassword = false)
    {
        NormalizeForm(model);

        if (!ValidEmployeeStatuses.Contains(
                model.Status,
                StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.Status),
                "Trạng thái chỉ được là Đang làm việc hoặc Đã nghỉ việc.");
        }

        if (model.HireDate.Date >
            DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(model.HireDate),
                "Ngày vào làm không được lớn hơn ngày hiện tại.");
        }

        var duplicateUserName =
            await _context.Users.AnyAsync(user =>
                user.UserName == model.UserName &&
                (!currentUserId.HasValue ||
                 user.UserId != currentUserId.Value));

        if (duplicateUserName)
        {
            ModelState.AddModelError(
                nameof(model.UserName),
                "Tên đăng nhập này đã tồn tại.");
        }

        var duplicateEmail =
            await _context.Users.AnyAsync(user =>
                user.Email == model.Email &&
                (!currentUserId.HasValue ||
                 user.UserId != currentUserId.Value));

        if (duplicateEmail)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email này đã được sử dụng.");
        }

        var duplicatePhone =
            await _context.Users.AnyAsync(user =>
                user.Phone == model.Phone &&
                (!currentUserId.HasValue ||
                 user.UserId != currentUserId.Value));

        if (duplicatePhone)
        {
            ModelState.AddModelError(
                nameof(model.Phone),
                "Số điện thoại này đã được sử dụng.");
        }

        if (requirePassword)
        {
            if (string.IsNullOrWhiteSpace(
                    model.Password))
            {
                ModelState.AddModelError(
                    nameof(model.Password),
                    "Vui lòng nhập mật khẩu.");
            }
            else if (model.Password.Length < 6)
            {
                ModelState.AddModelError(
                    nameof(model.Password),
                    "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (model.Password !=
                model.ConfirmPassword)
            {
                ModelState.AddModelError(
                    nameof(model.ConfirmPassword),
                    "Mật khẩu xác nhận không khớp.");
            }
        }
        else
        {
            ModelState.Remove(
                nameof(model.Password));

            ModelState.Remove(
                nameof(model.ConfirmPassword));

            if (!string.IsNullOrWhiteSpace(
                    model.Password))
            {
                if (model.Password.Length < 6)
                {
                    ModelState.AddModelError(
                        nameof(model.Password),
                        "Mật khẩu mới phải có ít nhất 6 ký tự.");
                }

                if (model.Password !=
                    model.ConfirmPassword)
                {
                    ModelState.AddModelError(
                        nameof(model.ConfirmPassword),
                        "Mật khẩu xác nhận không khớp.");
                }
            }
        }
    }

    // =========================================
    // DANH SÁCH NHÂN VIÊN
    // GET: /Employees
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        search =
            search?.Trim() ?? "";

        status =
            status?.Trim().ToLowerInvariant() ?? "";

        var query = _context.Employees
            .AsNoTracking()
            .Include(employee =>
                employee.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(employee =>
                employee.User.FullName.Contains(search) ||
                employee.User.UserName.Contains(search) ||
                employee.User.Email.Contains(search) ||
                employee.User.Phone.Contains(search) ||
                employee.Position.Contains(search));
        }

        if (status == "working")
        {
            query = query.Where(employee =>
                employee.Status == "Đang làm việc");
        }
        else if (status == "resigned")
        {
            query = query.Where(employee =>
                employee.Status == "Đã nghỉ việc");
        }
        else if (status == "inactive")
        {
            query = query.Where(employee =>
                employee.User.Status != "Hoạt động");
        }

        var employees =
            await query
                .OrderByDescending(employee =>
                    employee.EmployeeId)
                .Select(employee =>
                    new EmployeeManagementItemViewModel
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        UserId =
                            employee.UserId,

                        FullName =
                            employee.User.FullName,

                        UserName =
                            employee.User.UserName,

                        Email =
                            employee.User.Email,

                        Phone =
                            employee.User.Phone,

                        Position =
                            employee.Position,

                        HireDate =
                            employee.HireDate,

                        EmployeeStatus =
                            employee.Status,

                        AccountStatus =
                            employee.User.Status,

                        OrdersHandled =
                            employee.Orders.Count,

                        InvoicesCreated =
                            employee.Invoices.Count
                    })
                .ToListAsync();

        var model =
            new EmployeeManagementIndexViewModel
            {
                Search =
                    search,

                StatusFilter =
                    status,

                TotalEmployees =
                    await _context.Employees
                        .CountAsync(),

                WorkingEmployees =
                    await _context.Employees
                        .CountAsync(employee =>
                            employee.Status ==
                            "Đang làm việc"),

                ResignedEmployees =
                    await _context.Employees
                        .CountAsync(employee =>
                            employee.Status ==
                            "Đã nghỉ việc"),

                ActiveAccounts =
                    await _context.Employees
                        .CountAsync(employee =>
                            employee.User.Status ==
                            "Hoạt động"),

                Employees =
                    employees
            };

        return View(
            "~/Views/Admin/Employee/Index.cshtml",
            model);
    }

    // =========================================
    // CHI TIẾT NHÂN VIÊN
    // GET: /Employees/Details/5
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var employee =
            await _context.Employees
                .AsNoTracking()
                .Include(item => item.User)
                .FirstOrDefaultAsync(item =>
                    item.EmployeeId == id);

        if (employee == null)
        {
            return NotFound();
        }

        var orders =
            await _context.Orders
                .AsNoTracking()
                .Include(order => order.Customer)
                    .ThenInclude(customer =>
                        customer.User)
                .Where(order =>
                    order.EmployeeId == id)
                .OrderByDescending(order =>
                    order.OrderDate)
                .ToListAsync();

        var invoices =
            await _context.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.EmployeeId == id)
                .OrderByDescending(invoice =>
                    invoice.InvoiceDate)
                .ToListAsync();

        var model =
            new EmployeeManagementDetailsViewModel
            {
                EmployeeId =
                    employee.EmployeeId,

                UserId =
                    employee.UserId,

                FullName =
                    employee.User.FullName,

                UserName =
                    employee.User.UserName,

                Email =
                    employee.User.Email,

                Phone =
                    employee.User.Phone,

                Position =
                    employee.Position,

                HireDate =
                    employee.HireDate,

                EmployeeStatus =
                    employee.Status,

                AccountStatus =
                    employee.User.Status,

                CreatedAt =
                    employee.User.CreatedAt,

                TotalOrdersHandled =
                    orders.Count,

                CompletedOrders =
                    orders.Count(order =>
                        IsCompletedOrder(
                            order.Status)),

                TotalInvoices =
                    invoices.Count,

                ActiveInvoiceRevenue =
                    invoices
                        .Where(invoice =>
                            invoice.Status)
                        .Sum(invoice =>
                            invoice.TotalAmount),

                RecentOrders =
                    orders
                        .Take(10)
                        .Select(order =>
                            new EmployeeOrderSummaryViewModel
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
                                    order.Status
                            })
                        .ToList(),

                RecentInvoices =
                    invoices
                        .Take(10)
                        .Select(invoice =>
                            new EmployeeInvoiceSummaryViewModel
                            {
                                InvoiceId =
                                    invoice.InvoiceId,

                                OrderId =
                                    invoice.OrderId,

                                InvoiceDate =
                                    invoice.InvoiceDate,

                                TotalAmount =
                                    invoice.TotalAmount,

                                Status =
                                    invoice.Status
                            })
                        .ToList()
            };

        return View(
            "~/Views/Admin/Employee/Details.cshtml",
            model);
    }

    // =========================================
    // FORM THÊM NHÂN VIÊN
    // GET: /Employees/Create
    // =========================================

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var model =
            new EmployeeManagementFormViewModel
            {
                HireDate =
                    DateTime.Today,

                Status =
                    "Đang làm việc"
            };

        return View(
            "~/Views/Admin/Employee/Create.cshtml",
            model);
    }

    // =========================================
    // LƯU NHÂN VIÊN MỚI
    // POST: /Employees/Create
    // =========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        EmployeeManagementFormViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        await ValidateFormAsync(
            model,
            requirePassword: true);

        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Admin/Employee/Create.cshtml",
                model);
        }

        var employeeRole =
            await _context.Roles
                .FirstOrDefaultAsync(role =>
                    role.RoleName == "Employee" ||
                    role.RoleName == "Empolyee");

        if (employeeRole == null)
        {
            ModelState.AddModelError(
                "",
                "Database chưa có Role Employee.");

            return View(
                "~/Views/Admin/Employee/Create.cshtml",
                model);
        }

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            var user = new User
            {
                UserName =
                    model.UserName,

                FullName =
                    model.FullName,

                Email =
                    model.Email,

                Phone =
                    model.Phone,

                Password =
                    "",

                Status =
                    GetAccountStatus(
                        model.Status),

                CreatedAt =
                    DateTime.Now,

                RoleId =
                    employeeRole.RoleId
            };

            user.Password =
                _passwordHasher.HashPassword(
                    user,
                    model.Password);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            var employee = new Employee
            {
                UserId =
                    user.UserId,

                Position =
                    model.Position,

                HireDate =
                    model.HireDate,

                Status =
                    model.Status
            };

            _context.Employees.Add(employee);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                $"Đã thêm nhân viên “{model.FullName}”.";

            return RedirectToAction(
                nameof(Details),
                new { id = employee.EmployeeId });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                "",
                "Không thể thêm nhân viên. Vui lòng thử lại.");

            return View(
                "~/Views/Admin/Employee/Create.cshtml",
                model);
        }
    }

    // =========================================
    // FORM CHỈNH SỬA NHÂN VIÊN
    // GET: /Employees/Edit/5
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var employee =
            await _context.Employees
                .AsNoTracking()
                .Include(item => item.User)
                .FirstOrDefaultAsync(item =>
                    item.EmployeeId == id);

        if (employee == null)
        {
            return NotFound();
        }

        var model =
            new EmployeeManagementFormViewModel
            {
                EmployeeId =
                    employee.EmployeeId,

                UserId =
                    employee.UserId,

                FullName =
                    employee.User.FullName,

                UserName =
                    employee.User.UserName,

                Email =
                    employee.User.Email,

                Phone =
                    employee.User.Phone,

                Position =
                    employee.Position,

                HireDate =
                    employee.HireDate,

                Status =
                    employee.Status,

                Password =
                    "",

                ConfirmPassword =
                    ""
            };

        return View(
            "~/Views/Admin/Employee/Edit.cshtml",
            model);
    }

    // =========================================
    // LƯU CHỈNH SỬA
    // POST: /Employees/Edit/5
    // =========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EmployeeManagementFormViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        if (id != model.EmployeeId)
        {
            return NotFound();
        }

        var employee =
            await _context.Employees
                .Include(item => item.User)
                .FirstOrDefaultAsync(item =>
                    item.EmployeeId == id);

        if (employee == null)
        {
            return NotFound();
        }

        model.UserId =
            employee.UserId;

        await ValidateFormAsync(
            model,
            employee.UserId,
            requirePassword: false);

        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Admin/Employee/Edit.cshtml",
                model);
        }

        employee.Position =
            model.Position;

        employee.HireDate =
            model.HireDate;

        employee.Status =
            model.Status;

        employee.User.FullName =
            model.FullName;

        employee.User.UserName =
            model.UserName;

        employee.User.Email =
            model.Email;

        employee.User.Phone =
            model.Phone;

        employee.User.Status =
            GetAccountStatus(
                model.Status);

        if (!string.IsNullOrWhiteSpace(
                model.Password))
        {
            employee.User.Password =
                _passwordHasher.HashPassword(
                    employee.User,
                    model.Password);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Đã cập nhật nhân viên “{model.FullName}”.";

        return RedirectToAction(
            nameof(Details),
            new { id = employee.EmployeeId });
    }

    // =========================================
    // ĐỔI TRẠNG THÁI LÀM VIỆC
    // POST: /Employees/ToggleStatus
    // =========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var employee =
            await _context.Employees
                .Include(item => item.User)
                .FirstOrDefaultAsync(item =>
                    item.EmployeeId == id);

        if (employee == null)
        {
            return NotFound();
        }

        var isWorking =
            string.Equals(
                employee.Status,
                "Đang làm việc",
                StringComparison.OrdinalIgnoreCase);

        employee.Status =
            isWorking
                ? "Đã nghỉ việc"
                : "Đang làm việc";

        employee.User.Status =
            GetAccountStatus(
                employee.Status);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            employee.Status == "Đang làm việc"
                ? $"Đã cho nhân viên “{employee.User.FullName}” làm việc lại."
                : $"Đã chuyển nhân viên “{employee.User.FullName}” sang nghỉ việc.";

        return RedirectToAction(
            nameof(Index));
    }
}