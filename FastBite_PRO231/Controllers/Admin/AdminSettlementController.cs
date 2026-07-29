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

public class AdminSettlementController : Controller
{
    private readonly FastBiteDbContext _context;

    private readonly PasswordHasher<User> _passwordHasher = new();

    private static readonly string[] ValidShipperStatuses =
    {
        "Đang làm việc",
        "Đã nghỉ việc"
    };

    public AdminSettlementController(FastBiteDbContext context)
    {
        _context = context;
    }

    // =========================================
    // KIỂM TRA QUYỀN ADMIN
    // =========================================

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

        TempData["Error"] = "Chỉ tài khoản Admin mới được quản lý shipper.";
        return RedirectToAction("Index", "Home");
    }

    private static string GetAccountStatus(string shipperStatus)
    {
        return string.Equals(
            shipperStatus,
            "Đang làm việc",
            StringComparison.OrdinalIgnoreCase)
                ? "Hoạt động"
                : "Ngừng hoạt động";
    }

    private static bool IsCompletedOrder(string? status)
    {
        return string.Equals(status, "Hoàn thành", StringComparison.OrdinalIgnoreCase);
    }

    private void NormalizeForm(ShipperManagementFormViewModel model)
    {
        model.FullName = model.FullName?.Trim() ?? "";
        model.UserName = model.UserName?.Trim() ?? "";
        model.Email = model.Email?.Trim() ?? "";
        model.Phone = model.Phone?.Trim() ?? "";
        model.Status = model.Status?.Trim() ?? "";
        model.Password = model.Password?.Trim() ?? "";
        model.ConfirmPassword = model.ConfirmPassword?.Trim() ?? "";
    }

    private async Task ValidateFormAsync(
        ShipperManagementFormViewModel model,
        int? currentUserId = null,
        bool requirePassword = false)
    {
        NormalizeForm(model);

        if (!ValidShipperStatuses.Contains(model.Status, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.Status),
                "Trạng thái chỉ được là Đang làm việc hoặc Đã nghỉ việc.");
        }

        var duplicateUserName = await _context.Users.AnyAsync(user =>
            user.UserName == model.UserName &&
            (!currentUserId.HasValue || user.UserId != currentUserId.Value));

        if (duplicateUserName)
        {
            ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập này đã tồn tại.");
        }

        var duplicateEmail = await _context.Users.AnyAsync(user =>
            user.Email == model.Email &&
            (!currentUserId.HasValue || user.UserId != currentUserId.Value));

        if (duplicateEmail)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
        }

        var duplicatePhone = await _context.Users.AnyAsync(user =>
            user.Phone == model.Phone &&
            (!currentUserId.HasValue || user.UserId != currentUserId.Value));

        if (duplicatePhone)
        {
            ModelState.AddModelError(nameof(model.Phone), "Số điện thoại này đã được sử dụng.");
        }

        if (requirePassword)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Vui lòng nhập mật khẩu.");
            }
            else if (model.Password.Length < 6)
            {
                ModelState.AddModelError(nameof(model.Password), "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
            }
        }
        else
        {
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.ConfirmPassword));

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (model.Password.Length < 6)
                {
                    ModelState.AddModelError(nameof(model.Password), "Mật khẩu mới phải có ít nhất 6 ký tự.");
                }

                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
                }
            }
        }
    }

    // =========================================
    //Trang chủ
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status)
    {
        if (!IsAdmin()) return RedirectUnauthorized();

        search = search?.Trim() ?? "";
        status = status?.Trim().ToLowerInvariant() ?? "";

        var query = _context.Shippers
            .AsNoTracking()
            .Include(shipper => shipper.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(shipper =>
                shipper.User.FullName.Contains(search) ||
                shipper.User.UserName.Contains(search) ||
                shipper.User.Email.Contains(search) ||
                shipper.User.Phone.Contains(search));
        }

        if (status == "working")
        {
            query = query.Where(shipper => shipper.Status == "Đang làm việc");
        }
        else if (status == "resigned")
        {
            query = query.Where(shipper => shipper.Status == "Đã nghỉ việc");
        }
        else if (status == "inactive")
        {
            query = query.Where(shipper => shipper.User.Status != "Hoạt động");
        }

        var shippers = await query
            .OrderByDescending(shipper => shipper.ShipperId)
            .Select(shipper => new ShipperManagementItemViewModel
            {
                ShipperId = shipper.ShipperId,
                UserId = shipper.UserId,
                FullName = shipper.User.FullName,
                UserName = shipper.User.UserName,
                Email = shipper.User.Email,
                Phone = shipper.User.Phone,
                ShipperStatus = shipper.Status,
                AccountStatus = shipper.User.Status,
                OrdersHandled = shipper.Orders.Count,
                CompletedOrders = shipper.Orders.Count(order => order.Status == "Hoàn thành")
            })
            .ToListAsync();

        var model = new ShipperManagementIndexViewModel
        {
            Search = search,
            StatusFilter = status,

            TotalShippers = await _context.Shippers.CountAsync(),

            WorkingShippers = await _context.Shippers
                .CountAsync(shipper => shipper.Status == "Đang làm việc"),

            ResignedShippers = await _context.Shippers
                .CountAsync(shipper => shipper.Status == "Đã nghỉ việc"),

            ActiveAccounts = await _context.Shippers
                .CountAsync(shipper => shipper.User.Status == "Hoạt động"),

            Shippers = shippers
        };

        return View("~/Views/Admin/Shipper/Index.cshtml", model);
    }

    // =========================================
    // CHI TIẾT SHIPPER
    // GET: /Shippers/Details/5
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsAdmin()) return RedirectUnauthorized();

        var shipper = await _context.Shippers
            .AsNoTracking()
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.ShipperId == id);

        if (shipper == null) return NotFound();

        var orders = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
                .ThenInclude(customer => customer.User)
            .Where(order => order.ShipperId == id)
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync();

        var model = new ShipperManagementDetailsViewModel
        {
            ShipperId = shipper.ShipperId,
            UserId = shipper.UserId,
            FullName = shipper.User.FullName,
            UserName = shipper.User.UserName,
            Email = shipper.User.Email,
            Phone = shipper.User.Phone,
            ShipperStatus = shipper.Status,
            AccountStatus = shipper.User.Status,
            CreatedAt = shipper.User.CreatedAt,

            TotalOrdersHandled = orders.Count,

            CompletedOrders = orders.Count(order => IsCompletedOrder(order.Status)),

            PendingSettlementAmount = orders
                .Where(order =>
                    order.PaymentMethod == "COD" &&
                    order.PaymentStatus == "Đã thanh toán" &&
                    order.SettlementStatus == "Chưa đối soát")
                .Sum(order => order.TotalAmount),

            RecentOrders = orders
                .Take(10)
                .Select(order => new ShipperOrderSummaryViewModel
                {
                    OrderId = order.OrderId,
                    CustomerName = order.Customer.User.FullName,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    PaymentMethod = order.PaymentMethod,
                    PaymentStatus = order.PaymentStatus
                })
                .ToList()
        };

        return View("~/Views/Admin/Shipper/Details.cshtml", model);
    }

    // =========================================
    // FORM THÊM SHIPPER
    // GET: /Shippers/Create
    // =========================================

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsAdmin()) return RedirectUnauthorized();

        var model = new ShipperManagementFormViewModel
        {
            Status = "Đang làm việc"
        };

        return View("~/Views/Admin/Shipper/Create.cshtml", model);
    }

    // =========================================
    // LƯU SHIPPER MỚI
    // POST: /Shippers/Create
    // =========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShipperManagementFormViewModel model)
    {
        if (!IsAdmin()) return RedirectUnauthorized();

        await ValidateFormAsync(model, requirePassword: true);

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Shipper/Create.cshtml", model);
        }

        var shipperRole = await _context.Roles
            .FirstOrDefaultAsync(role => role.RoleName == "Shipper");

        if (shipperRole == null)
        {
            ModelState.AddModelError("", "Database chưa có Role Shipper.");
            return View("~/Views/Admin/Shipper/Create.cshtml", model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = new User
            {
                UserName = model.UserName,
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Password = "",
                Status = GetAccountStatus(model.Status),
                CreatedAt = DateTime.Now,
                RoleId = shipperRole.RoleId
            };

            user.Password = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var shipper = new Shipper
            {
                UserId = user.UserId,
                Status = model.Status
            };

            _context.Shippers.Add(shipper);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đã thêm shipper “{model.FullName}”.";

            return RedirectToAction(nameof(Details), new { id = shipper.ShipperId });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError("", "Không thể thêm shipper. Vui lòng thử lại.");
            return View("~/Views/Admin/Shipper/Create.cshtml", model);
        }
    }

    // =========================================
    // Sửa
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin()) return RedirectUnauthorized();

        var shipper = await _context.Shippers
            .AsNoTracking()
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.ShipperId == id);

        if (shipper == null) return NotFound();

        var model = new ShipperManagementFormViewModel
        {
            ShipperId = shipper.ShipperId,
            UserId = shipper.UserId,
            FullName = shipper.User.FullName,
            UserName = shipper.User.UserName,
            Email = shipper.User.Email,
            Phone = shipper.User.Phone,
            Status = shipper.Status,
            Password = "",
            ConfirmPassword = ""
        };

        return View("~/Views/Admin/Shipper/Edit.cshtml", model);
    }

    // =========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ShipperManagementFormViewModel model)
    {
        if (!IsAdmin()) return RedirectUnauthorized();

        if (id != model.ShipperId) return NotFound();

        var shipper = await _context.Shippers
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.ShipperId == id);

        if (shipper == null) return NotFound();

        model.UserId = shipper.UserId;

        await ValidateFormAsync(model, shipper.UserId, requirePassword: false);

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/Shipper/Edit.cshtml", model);
        }

        shipper.Status = model.Status;

        shipper.User.FullName = model.FullName;
        shipper.User.UserName = model.UserName;
        shipper.User.Email = model.Email;
        shipper.User.Phone = model.Phone;
        shipper.User.Status = GetAccountStatus(model.Status);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            shipper.User.Password = _passwordHasher.HashPassword(shipper.User, model.Password);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật shipper “{model.FullName}”.";

        return RedirectToAction(nameof(Details), new { id = shipper.ShipperId });
    }

    // =========================================
    // ĐỔI TRẠNG THÁI LÀM VIỆC
    // POST: /Shippers/ToggleStatus
    // =========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        if (!IsAdmin()) return RedirectUnauthorized();

        var shipper = await _context.Shippers
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.ShipperId == id);

        if (shipper == null) return NotFound();

        var isWorking = string.Equals(
            shipper.Status,
            "Đang làm việc",
            StringComparison.OrdinalIgnoreCase);

        shipper.Status = isWorking ? "Đã nghỉ việc" : "Đang làm việc";
        shipper.User.Status = GetAccountStatus(shipper.Status);

        await _context.SaveChangesAsync();

        TempData["Success"] = shipper.Status == "Đang làm việc"
            ? $"Đã cho shipper “{shipper.User.FullName}” làm việc lại."
            : $"Đã chuyển shipper “{shipper.User.FullName}” sang nghỉ việc.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================
    // TRANG ĐỐI SOÁT — liệt kê tiền từng shipper cần nộp
    // GET: /Shippers/Settlement
    // =========================================
    [HttpGet]
    public async Task<IActionResult> Settlement()
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Login");

        var pendingOrders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Shipper).ThenInclude(s => s!.User)
            .Where(o =>
                o.PaymentMethod == "COD" &&
                o.PaymentStatus == "Đã thanh toán" &&
                o.SettlementStatus == "Chưa đối soát" &&
                o.ShipperId != null)
            .ToListAsync();

        var groups = pendingOrders
            .GroupBy(o => o.ShipperId!.Value)
            .Select(g => new SettlementGroupViewModel
            {
                ShipperId = g.Key,
                ShipperName = g.First().Shipper?.User.FullName ?? "Shipper",
                ShipperPhone = g.First().Shipper?.User.Phone ?? "",
                OrderCount = g.Count(),
                TotalAmount = g.Sum(o => o.TotalAmount),
                OrderIds = g.Select(o => o.OrderId).ToList()
            })
            .OrderByDescending(g => g.TotalAmount)
            .ToList();

        var model = new SettlementViewModel { Groups = groups };
        return View("~/Views/Admin/Shipper/Settlement.cshtml", model);
    }

    // =========================================
    // XÁC NHẬN ĐÃ NHẬN TIỀN TỪ 1 SHIPPER
    // POST: /Shippers/ConfirmSettlement
    // =========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmSettlement(List<int> orderIds)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Login");

        if (orderIds == null || orderIds.Count == 0)
        {
            TempData["Error"] = "Không có đơn hàng nào được chọn.";
            return RedirectToAction(nameof(Settlement));
        }

        var orders = await _context.Orders
            .Where(o => orderIds.Contains(o.OrderId))
            .ToListAsync();

        foreach (var order in orders)
        {
            order.SettlementStatus = "Đã đối soát";
            order.SettledAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xác nhận đối soát {orders.Count} đơn hàng.";
        return RedirectToAction(nameof(Settlement));
    }

    // =========================================
    // FORM GÁN SHIPPER CHO 1 ĐƠN
    // GET: /AdminOrder/AssignShipper?orderId=5
    // =========================================
    [HttpGet]
    public async Task<IActionResult> AssignShipper(int orderId)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Login");

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer).ThenInclude(c => c.User)
            .Include(o => o.Shipper)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null) return NotFound();

        var shippers = await _context.Shippers
            .AsNoTracking()
            .Where(s => s.Status == "Đang làm việc")
            .Select(s => new ShipperOptionViewModel
            {
                ShipperId = s.ShipperId,
                FullName = s.User.FullName,
                ActiveOrderCount = s.Orders.Count(o =>
                    o.Status != "Hoàn thành" && o.Status != "Đã huỷ")
            })
            .ToListAsync();

        var model = new AssignShipperViewModel
        {
            OrderId = order.OrderId,
            CustomerName = order.Customer.User?.FullName ?? "Khách hàng",
            DeliveryAddress = order.DeliveryAddress ?? "",
            CurrentShipperId = order.ShipperId,
            CurrentShipperName = order.Shipper?.User.FullName,
            AvailableShippers = shippers
        };

        return View(model);
    }

    // =========================================
    // XÁC NHẬN GÁN SHIPPER
    // POST: /AdminOrder/AssignShipper
    // =========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignShipper(int orderId, int shipperId)
    {
        if (!IsAdmin()) return RedirectToAction("Login", "Login");

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            TempData["Error"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction("Index");
        }

        var shipperExists = await _context.Shippers
            .AnyAsync(s => s.ShipperId == shipperId && s.Status == "Đang làm việc");

        if (!shipperExists)
        {
            TempData["Error"] = "Shipper không hợp lệ hoặc đã ngừng làm việc.";
            return RedirectToAction(nameof(AssignShipper), new { orderId });
        }

        // Admin có quyền GÁN LẠI kể cả đơn đã có shipper khác 
        // (khác với shipper tự nhận — chỉ cho nhận đơn còn trống)
        order.ShipperId = shipperId;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã gán đơn #{order.OrderId} cho shipper.";
        return RedirectToAction("Index");
    }
}