using System;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Common;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers.Admin;

public class UsersController : Controller
{
    private readonly FastBiteDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UsersController(FastBiteDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin()
    {
        var role = HttpContext.Session.GetString("Role");
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectUnauthorized()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue)
        {
            TempData["Error"] = "Vui lòng đăng nhập tài khoản Admin.";
            return RedirectToAction("Login", "Login");
        }

        TempData["Error"] = "Chỉ tài khoản Admin mới được quản lý tài khoản.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? role, string? status)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        search = search?.Trim() ?? "";
        role = role?.Trim().ToLowerInvariant() ?? "";
        status = status?.Trim().ToLowerInvariant() ?? "";

        var query = _context.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Include(user => user.Customer)
            .Include(user => user.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user =>
                user.UserName.Contains(search) ||
                user.FullName.Contains(search) ||
                user.Email.Contains(search) ||
                user.Phone.Contains(search));
        }

        if (role == "admin")
        {
            query = query.Where(user => user.Role.RoleName == "Admin");
        }
        else if (role == "employee")
        {
            query = query.Where(user => user.Role.RoleName == "Employee");
        }
        else if (role == "shipper")   // MỚI
        {
            query = query.Where(user => user.Role.RoleName == "Shipper");
        }
        else if (role == "customer")
        {
            query = query.Where(user => user.Role.RoleName == "Customer");
        }

        if (status == "active")
        {
            query = query.Where(user => user.Status == OrderStatusConstants.AccountActive);
        }
        else if (status == "inactive")
        {
            query = query.Where(user => user.Status != OrderStatusConstants.AccountActive);
        }

        var userEntities = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenByDescending(user => user.UserId)
            .ToListAsync();

        var model = new UserManagementIndexViewModel
        {
            Search = search,
            RoleFilter = role,
            StatusFilter = status,

            TotalUsers = await _context.Users.CountAsync(),

            ActiveUsers = await _context.Users.CountAsync(user =>
                user.Status == OrderStatusConstants.AccountActive),

            InactiveUsers = await _context.Users.CountAsync(user =>
                user.Status != OrderStatusConstants.AccountActive),

            AdminUsers = await _context.Users.CountAsync(user => user.Role.RoleName == "Admin"),
            EmployeeUsers = await _context.Users.CountAsync(user => user.Role.RoleName == "Employee"),
            CustomerUsers = await _context.Users.CountAsync(user => user.Role.RoleName == "Customer"),

            Users = userEntities
                .Select(user => new UserManagementItemViewModel
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    RoleName = user.Role.RoleName,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                    HasCustomerProfile = user.Customer != null,
                    HasEmployeeProfile = user.Employee != null
                })
                .ToList()
        };

        return View("~/Views/Admin/User/Index.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .Include(item => item.Customer)
            .Include(item => item.Employee)
            .FirstOrDefaultAsync(item => item.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        var model = new UserManagementDetailsViewModel
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RoleName = user.Role.RoleName,
            Status = user.Status,
            CreatedAt = user.CreatedAt,

            CustomerId = user.Customer?.CustomerId,
            CustomerAddress = user.Customer?.Address ?? "",
            CustomerPoint = user.Customer?.Point ?? 0,

            EmployeeId = user.Employee?.EmployeeId,
            EmployeePosition = user.Employee?.Position ?? "",
            EmployeeHireDate = user.Employee?.HireDate,
            EmployeeStatus = user.Employee?.Status ?? ""
        };

        return View("~/Views/Admin/User/Details.cshtml", model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        return View("~/Views/Admin/User/Create.cshtml", new AdminAccountCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminAccountCreateViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        model.UserName = model.UserName?.Trim() ?? "";
        model.FullName = model.FullName?.Trim() ?? "";
        model.Email = model.Email?.Trim() ?? "";
        model.Phone = model.Phone?.Trim() ?? "";

        var userNameExists = await _context.Users.AnyAsync(user => user.UserName == model.UserName);
        if (userNameExists)
        {
            ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập này đã tồn tại.");
        }

        var emailExists = await _context.Users.AnyAsync(user => user.Email == model.Email);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
        }

        var phoneExists = await _context.Users.AnyAsync(user => user.Phone == model.Phone);
        if (phoneExists)
        {
            ModelState.AddModelError(nameof(model.Phone), "Số điện thoại này đã được sử dụng.");
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/User/Create.cshtml", model);
        }

        var adminRole = await _context.Roles.FirstOrDefaultAsync(item => item.RoleName == "Admin");

        if (adminRole == null)
        {
            ModelState.AddModelError("", "Database chưa có quyền Admin.");
            return View("~/Views/Admin/User/Create.cshtml", model);
        }

        try
        {
            var user = new User
            {
                UserName = model.UserName,
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Password = "",
                Status = OrderStatusConstants.AccountActive,
                CreatedAt = DateTime.Now,
                RoleId = adminRole.RoleId
            };

            user.Password = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo tài khoản Admin “{user.UserName}”.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không thể tạo tài khoản. Vui lòng kiểm tra lại dữ liệu.");
            return View("~/Views/Admin/User/Create.cshtml", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        var model = new UserManagementEditViewModel
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RoleName = user.Role.RoleName,
            Status = user.Status
        };

        return View("~/Views/Admin/User/Edit.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserManagementEditViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        if (id != model.UserId)
        {
            return NotFound();
        }

        model.UserName = model.UserName?.Trim() ?? "";
        model.FullName = model.FullName?.Trim() ?? "";
        model.Email = model.Email?.Trim() ?? "";
        model.Phone = model.Phone?.Trim() ?? "";
        model.Status = model.Status?.Trim() ?? "";

        var user = await _context.Users
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        model.RoleName = user.Role.RoleName;

        if (!OrderStatusConstants.ValidAccountStatuses.Contains(model.Status, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Status), "Trạng thái tài khoản không hợp lệ.");
        }

        var currentUserId = HttpContext.Session.GetInt32("UserId");

        if (currentUserId == user.UserId && model.Status != OrderStatusConstants.AccountActive)
        {
            ModelState.AddModelError(nameof(model.Status), "Bạn không thể tự khóa tài khoản đang đăng nhập.");
        }

        var duplicateUserName = await _context.Users.AnyAsync(item =>
            item.UserName == model.UserName && item.UserId != model.UserId);

        if (duplicateUserName)
        {
            ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập này đã tồn tại.");
        }

        var duplicateEmail = await _context.Users.AnyAsync(item =>
            item.Email == model.Email && item.UserId != model.UserId);

        if (duplicateEmail)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
        }

        var duplicatePhone = await _context.Users.AnyAsync(item =>
            item.Phone == model.Phone && item.UserId != model.UserId);

        if (duplicatePhone)
        {
            ModelState.AddModelError(nameof(model.Phone), "Số điện thoại này đã được sử dụng.");
        }

        if (user.Role.RoleName == "Admin" &&
            user.Status == OrderStatusConstants.AccountActive &&
            model.Status != OrderStatusConstants.AccountActive)
        {
            var activeAdminCount = await _context.Users.CountAsync(item =>
                item.Role.RoleName == "Admin" && item.Status == OrderStatusConstants.AccountActive);

            if (activeAdminCount <= 1)
            {
                ModelState.AddModelError(nameof(model.Status), "Không thể khóa Admin hoạt động cuối cùng.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/User/Edit.cshtml", model);
        }

        user.UserName = model.UserName;
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.Phone = model.Phone;
        user.Status = model.Status;

        await _context.SaveChangesAsync();

        if (currentUserId == user.UserId)
        {
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("FullName", user.FullName);
        }

        TempData["Success"] = $"Đã cập nhật tài khoản “{user.UserName}”.";
        return RedirectToAction(nameof(Details), new { id = user.UserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var user = await _context.Users
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = HttpContext.Session.GetInt32("UserId");

        if (currentUserId == user.UserId)
        {
            TempData["Error"] = "Bạn không thể tự khóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        var isCurrentlyActive = user.Status == OrderStatusConstants.AccountActive;

        if (isCurrentlyActive && user.Role.RoleName == "Admin")
        {
            var activeAdminCount = await _context.Users.CountAsync(item =>
                item.Role.RoleName == "Admin" && item.Status == OrderStatusConstants.AccountActive);

            if (activeAdminCount <= 1)
            {
                TempData["Error"] = "Không thể khóa Admin hoạt động cuối cùng.";
                return RedirectToAction(nameof(Index));
            }
        }

        user.Status = isCurrentlyActive
            ? OrderStatusConstants.AccountInactive
            : OrderStatusConstants.AccountActive;

        await _context.SaveChangesAsync();

        TempData["Success"] = user.Status == OrderStatusConstants.AccountActive
            ? $"Đã mở tài khoản “{user.UserName}”."
            : $"Đã khóa tài khoản “{user.UserName}”.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        var model = new AdminResetPasswordViewModel
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FullName = user.FullName
        };

        return View("~/Views/Admin/User/ResetPassword.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(AdminResetPasswordViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var user = await _context.Users.FirstOrDefaultAsync(item => item.UserId == model.UserId);

        if (user == null)
        {
            return NotFound();
        }

        model.UserName = user.UserName;
        model.FullName = user.FullName;

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/User/ResetPassword.cshtml", model);
        }

        user.Password = _passwordHasher.HashPassword(user, model.NewPassword);

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã đặt lại mật khẩu cho tài khoản “{user.UserName}”.";
        return RedirectToAction(nameof(Details), new { id = user.UserId });
    }
}