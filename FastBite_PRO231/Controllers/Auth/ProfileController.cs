using System;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers.Auth;

public class ProfileController : Controller
{
    private readonly FastBiteDbContext _context;

    public ProfileController(
        FastBiteDbContext context)
    {
        _context = context;
    }

    // =========================================
    // KIỂM TRA ĐÃ ĐĂNG NHẬP
    // =========================================

    private int? GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32(
            "UserId");
    }

    private IActionResult RedirectToLogin()
    {
        TempData["Error"] =
            "Vui lòng đăng nhập để xem hồ sơ.";

        return RedirectToAction(
            "Login",
            "Login");
    }

    // =========================================
    // NẠP THÔNG TIN KHÔNG ĐƯỢC PHÉP SỬA
    // =========================================

    private async Task<bool> LoadSystemInformationAsync(
        ProfileViewModel model,
        int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .Include(item => item.Customer)
            .Include(item => item.Employee)
            .FirstOrDefaultAsync(item =>
                item.UserId == userId);

        if (user == null)
        {
            return false;
        }

        model.UserId =
            user.UserId;

        model.RoleName =
            user.Role?.RoleName ?? "";

        model.AccountStatus =
            user.Status;

        model.CreatedAt =
            user.CreatedAt;

        model.CustomerId =
            user.Customer?.CustomerId;

        model.EmployeeId =
            user.Employee?.EmployeeId;

        model.Point =
            user.Customer?.Point ?? 0;

        model.Position =
            user.Employee?.Position ?? "";

        model.HireDate =
            user.Employee?.HireDate;

        model.EmployeeStatus =
            user.Employee?.Status ?? "";

        return true;
    }

    // =========================================
    // XEM VÀ SỬA HỒ SƠ
    // GET: /Profile
    // =========================================

    [HttpGet]
    [Route("Profile")]
    public async Task<IActionResult> Index()
    {
        var userId =
            GetCurrentUserId();

        if (!userId.HasValue)
        {
            return RedirectToLogin();
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .Include(item => item.Customer)
            .Include(item => item.Employee)
            .FirstOrDefaultAsync(item =>
                item.UserId == userId.Value);

        if (user == null)
        {
            HttpContext.Session.Clear();

            return RedirectToLogin();
        }

        var fullName =
            !string.IsNullOrWhiteSpace(
                user.Customer?.User.FullName)
                ? user.Customer.User.FullName
                : user.FullName;

        var model = new ProfileViewModel
        {
            UserId =
                user.UserId,

            CustomerId =
                user.Customer?.CustomerId,

            EmployeeId =
                user.Employee?.EmployeeId,

            FullName =
                fullName,

            UserName =
                user.UserName,

            Email =
                user.Email,

            Phone =
                user.Phone,

            Address =
                user.Customer?.Address ?? "",

            RoleName =
                user.Role?.RoleName ?? "",

            AccountStatus =
                user.Status,

            CreatedAt =
                user.CreatedAt,

            Point =
                user.Customer?.Point ?? 0,

            Position =
                user.Employee?.Position ?? "",

            HireDate =
                user.Employee?.HireDate,

            EmployeeStatus =
                user.Employee?.Status ?? ""
        };

        return View(
            "~/Views/Profile/Index.cshtml",
            model);
    }

    // =========================================
    // LƯU THAY ĐỔI HỒ SƠ
    // POST: /Profile
    // =========================================

    [HttpPost]
    [Route("Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        ProfileViewModel model)
    {
        var currentUserId =
            GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return RedirectToLogin();
        }

        if (model.UserId !=
            currentUserId.Value)
        {
            return Forbid();
        }

        model.FullName =
            model.FullName?.Trim() ?? "";

        model.UserName =
            model.UserName?.Trim() ?? "";

        model.Email =
            model.Email?.Trim() ?? "";

        model.Phone =
            model.Phone?.Trim() ?? "";

        model.Address =
            model.Address?.Trim() ?? "";

        var systemInformationLoaded =
            await LoadSystemInformationAsync(
                model,
                currentUserId.Value);

        if (!systemInformationLoaded)
        {
            HttpContext.Session.Clear();

            return RedirectToLogin();
        }

        if (model.IsCustomer &&
            string.IsNullOrWhiteSpace(
                model.Address))
        {
            ModelState.AddModelError(
                nameof(model.Address),
                "Vui lòng nhập địa chỉ nhận hàng.");
        }

        var duplicateUserName =
            await _context.Users
                .AsNoTracking()
                .AnyAsync(user =>
                    user.UserName ==
                        model.UserName &&
                    user.UserId !=
                        currentUserId.Value);

        if (duplicateUserName)
        {
            ModelState.AddModelError(
                nameof(model.UserName),
                "Tên đăng nhập này đã tồn tại.");
        }

        var duplicateEmail =
            await _context.Users
                .AsNoTracking()
                .AnyAsync(user =>
                    user.Email ==
                        model.Email &&
                    user.UserId !=
                        currentUserId.Value);

        if (duplicateEmail)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email này đã được sử dụng.");
        }

        var duplicatePhone =
            await _context.Users
                .AsNoTracking()
                .AnyAsync(user =>
                    user.Phone ==
                        model.Phone &&
                    user.UserId !=
                        currentUserId.Value);

        if (duplicatePhone)
        {
            ModelState.AddModelError(
                nameof(model.Phone),
                "Số điện thoại này đã được sử dụng.");
        }

        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Profile/Index.cshtml",
                model);
        }

        var user = await _context.Users
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item =>
                item.UserId ==
                currentUserId.Value);

        if (user == null)
        {
            HttpContext.Session.Clear();

            return RedirectToLogin();
        }

        try
        {
            user.FullName =
                model.FullName;

            user.UserName =
                model.UserName;

            user.Email =
                model.Email;

            user.Phone =
                model.Phone;

            if (user.Customer != null)
            {
                user.Customer.User.FullName =
                    model.FullName;

                user.Customer.Address =
                    model.Address;
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString(
                "FullName",
                user.FullName);

            HttpContext.Session.SetString(
                "UserName",
                user.UserName);

            TempData["Success"] =
                "Đã cập nhật hồ sơ cá nhân thành công.";

            return RedirectToAction(
                nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "Không thể lưu hồ sơ. Vui lòng kiểm tra lại thông tin.");

            return View(
                "~/Views/Profile/Index.cshtml",
                model);
        }
    }
}