using FastBite_PRO231.Models;
using FastBite_PRO231.Services;
using FastBite_PRO231.ViewModels;
using FastBite_PRO231.ViewModels.Login;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FastBite_PRO231.Controllers.Auth;


public class LoginController : Controller
{
    private readonly FastBiteDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;

    public LoginController(
        FastBiteDbContext context,
        IMemoryCache cache,
        IEmailService emailService)
    {
        _context = context;
        _cache = cache;
        _emailService = emailService;
    }

    // Chuẩn hóa tên quyền
    private static string NormalizeRole(string roleName)
    {
        return roleName.Trim() switch
        {
            "Khách hàng" => "Customer",
            "Nhân viên" => "Employee",
            _ => roleName.Trim()
        };
    }

    //Chuyển trang theo quyền
    private IActionResult RedirectByRole(string? roleName)
    {
        var normalizedRole = NormalizeRole(roleName ?? "");

        return normalizedRole switch
        {
            "Admin" => RedirectToAction("Index", "AdminDashboard"),
            "Employee" => RedirectToAction("Index", "EmployeeHome"),
            "Shipper" => RedirectToAction("Index", "ShipperHome"),
            _ => RedirectToAction("Index", "CustomerHome")
        };
    }

    //Ktra tài khoản bị khóa
    private static bool IsDisabledUser(string status)
    {
        string[] disabledStatuses =
        {
            "Ngừng hoạt động",
            "Không hoạt động",
            "Đã khóa",
            "Inactive",
            "Locked"
        };

        return disabledStatuses.Any(item =>
            item.Equals(
                status,
                StringComparison.OrdinalIgnoreCase));
    }

    // KIỂM TRA MẬT KHẨU
    // Hỗ trợ cả mật khẩu cũ dạng chữ thường và mật khẩu hash.
    private PasswordVerificationResult VerifyPassword(
        User user,
        string enteredPassword)
    {
        try
        {
            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.Password,
                enteredPassword);

            if (result != PasswordVerificationResult.Failed)
            {
                return result;
            }
        }
        catch (FormatException)
        {
            // Mật khẩu cũ chưa được hash.
        }

        return user.Password == enteredPassword
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }

    //Đăng nhập
    [HttpGet]
    public IActionResult Login()
    {
        var sessionRole = HttpContext.Session.GetString("Role");

        if (!string.IsNullOrWhiteSpace(sessionRole))
        {
            return RedirectByRole(sessionRole);
        }

        return View(
            "~/Views/Login/Login.cshtml",
            new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.UserName = model.UserName?.Trim() ?? "";
        model.Password = model.Password?.Trim() ?? "";

        if (!ModelState.IsValid)
        {
            return View("~/Views/Login/Login.cshtml", model);
        }

        var user = await _context.Users
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item =>
                item.UserName == model.UserName);

        if (user == null)
        {
            Console.WriteLine("❌ Không tìm thấy user với UserName = '" + model.UserName + "'");
            ModelState.AddModelError( "", "Tên đăng nhập hoặc mật khẩu không chính xác."); 
            return View("~/Views/Login/Login.cshtml", model);
        }

        if (IsDisabledUser(user.Status))
        {
            ModelState.AddModelError("", "Tài khoản này đang bị khóa hoặc ngừng hoạt động.");
            return View("~/Views/Login/Login.cshtml", model);
        }

        if (user.Role == null)
        {
            ModelState.AddModelError("", "Tài khoản chưa được phân quyền.");
            return View("~/Views/Login/Login.cshtml", model);
        }

        var passwordResult = VerifyPassword(user, model.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            Console.WriteLine("❌ Password nhập = '" + model.Password + "', Password DB = '" + user.Password + "'");
            ModelState.AddModelError(
                "",
                "Tên đăng nhập hoặc mật khẩu không chính xác.");

            return View("~/Views/Login/Login.cshtml", model);
        }

        // Tự động nâng cấp mật khẩu cũ sang mật khẩu hash.
        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Password = _passwordHasher.HashPassword(user, model.Password);

            await _context.SaveChangesAsync();
        }

        var normalizedRole = NormalizeRole(
            user.Role.RoleName);

        HttpContext.Session.Clear();

        HttpContext.Session.SetInt32("UserId", user.UserId);

        HttpContext.Session.SetString("UserName", user.UserName);

        HttpContext.Session.SetString(
            "FullName",
            string.IsNullOrWhiteSpace(user.FullName)
                ? user.UserName
                : user.FullName);

        HttpContext.Session.SetString(
            "Role",
            normalizedRole);

        TempData["Success"] =
            $"Đăng nhập thành công. Xin chào {user.FullName}!";

        return RedirectByRole(normalizedRole);
    }

    //Đăng ký
    [HttpGet]
    public IActionResult Register()
    {
        return View(
            "~/Views/Login/Register.cshtml",
            new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.UserName = model.UserName.Trim();
        model.FullName = model.FullName.Trim();
        model.Email = model.Email.Trim();
        model.Phone = model.Phone.Trim();
        model.Address = model.Address.Trim();

        var userNameExists = await _context.Users
            .AnyAsync(user =>
                user.UserName == model.UserName);

        if (userNameExists)
        {
            ModelState.AddModelError(
                nameof(model.UserName),
                "Tên đăng nhập đã tồn tại.");

            return View(model);
        }

        var emailExists = await _context.Users
            .AnyAsync(user =>
                user.Email == model.Email);

        if (emailExists)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email đã được sử dụng.");

            return View(model);
        }

        var phoneExists = await _context.Users
            .AnyAsync(user =>
                user.Phone == model.Phone);

        if (phoneExists)
        {
            ModelState.AddModelError(
                nameof(model.Phone),
                "Số điện thoại đã được sử dụng.");

            return View(model);
        }

        var customerRole = await _context.Roles
            .FirstOrDefaultAsync(role =>
                role.RoleName == "Customer");

        if (customerRole == null)
        {
            ModelState.AddModelError(
                "",
                "Không tìm thấy vai trò Customer trong database.");

            return View(model);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var user = new User
            {
                UserName = model.UserName,
                Password = "",
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Status = "Hoạt động",
                CreatedAt = DateTime.Now,
                RoleId = customerRole.RoleId
            };

            user.Password = _passwordHasher.HashPassword(
                user,
                model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var customer = new Customer
            {
                UserId = user.UserId,
                Address = model.Address,
                Point = 0
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var cart = new Cart
            {
                CustomerId = customer.CustomerId,
                CreatedAt = DateTime.Now,
                TotalPrice = 0
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["Success"] =
                "Đăng ký thành công. Bạn hãy đăng nhập.";

            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                "",
                "Không thể tạo tài khoản. " +
                "Vui lòng kiểm tra lại dữ liệu.");

            Console.WriteLine(ex.Message);

            return View(model);
        }
    }

    // =========================================
    // BƯỚC 1: NHẬP EMAIL, GỬI OTP
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View("~/Views/Login/ForgotPassword.cshtml", new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        model.Email = model.Email?.Trim().ToLower() ?? "";

        if (!ModelState.IsValid)
        {
            return View("~/Views/Login/ForgotPassword.cshtml", model);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null)
        {
            // Không tiết lộ email có tồn tại hay không, tránh lộ thông tin tài khoản
            ModelState.AddModelError("", "Không tìm thấy tài khoản với email này.");
            return View("~/Views/Login/ForgotPassword.cshtml", model);
        }

        var otp = new Random().Next(100000, 999999).ToString();

        _cache.Set($"otp:{model.Email}", otp, TimeSpan.FromMinutes(5));

        try
        {
            await _emailService.SendOtpEmailAsync(model.Email, otp);
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Không thể gửi email. Vui lòng thử lại sau.");
            return View("~/Views/Login/ForgotPassword.cshtml", model);
        }

        TempData["Success"] = $"Đã gửi mã OTP tới {model.Email}.";

        return RedirectToAction(nameof(VerifyOtp), new { email = model.Email });
    }

    // =========================================
    // BƯỚC 2: NHẬP OTP
    [HttpGet]
    public IActionResult VerifyOtp(string email)
    {
        return View("~/Views/Login/VerifyOtp.cshtml", new VerifyOtpViewModel { Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyOtp(VerifyOtpViewModel model)
    {
        model.Email = model.Email?.Trim().ToLower() ?? "";
        model.Otp = model.Otp?.Trim() ?? "";

        if (!ModelState.IsValid)
        {
            return View("~/Views/Login/VerifyOtp.cshtml", model);
        }

        if (!_cache.TryGetValue($"otp:{model.Email}", out string? savedOtp) ||
            savedOtp != model.Otp)
        {
            ModelState.AddModelError(nameof(model.Otp), "Mã OTP không đúng hoặc đã hết hạn.");
            return View("~/Views/Login/VerifyOtp.cshtml", model);
        }

        // OTP đúng -> xoá OTP (dùng 1 lần), cấp 1 token tạm để bước 3 xác nhận
        _cache.Remove($"otp:{model.Email}");

        var resetToken = Guid.NewGuid().ToString("N");
        _cache.Set($"resettoken:{model.Email}", resetToken, TimeSpan.FromMinutes(10));

        return RedirectToAction(nameof(ResetPassword), new { email = model.Email, token = resetToken });
    }

    // =========================================
    // BƯỚC 3: ĐẶT MẬT KHẨU MỚI
    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        if (!_cache.TryGetValue($"resettoken:{email}", out string? savedToken) ||
            savedToken != token)
        {
            TempData["Error"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        var model = new ResetPasswordViewModel { Email = email, ResetToken = token };
        return View("~/Views/Login/ResetPassword.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Login/ResetPassword.cshtml", model);
        }

        if (!_cache.TryGetValue($"resettoken:{model.Email}", out string? savedToken) ||
            savedToken != model.ResetToken)
        {
            TempData["Error"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null)
        {
            TempData["Error"] = "Không tìm thấy tài khoản.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        user.Password = _passwordHasher.HashPassword(user, model.NewPassword);
        await _context.SaveChangesAsync();

        _cache.Remove($"resettoken:{model.Email}");

        TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }


    //Đổi mật khẩu
    [HttpGet]
    public IActionResult ChangePassword()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            TempData["Error"] =
                "Vui lòng đăng nhập trước khi đổi mật khẩu.";

            return RedirectToAction(nameof(Login));
        }

        return View(
            "~/Views/Login/ChangePassword.cshtml",
            new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            TempData["Error"] =
                "Phiên đăng nhập đã hết hạn.";

            return RedirectToAction(nameof(Login));
        }

        model.OldPassword ??= "";
        model.NewPassword ??= "";
        model.ConfirmPassword ??= "";

        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Login/ChangePassword.cshtml",
                model);
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(item =>
                item.UserId == userId.Value);

        if (user == null)
        {
            HttpContext.Session.Clear();

            TempData["Error"] =
                "Không tìm thấy tài khoản.";

            return RedirectToAction(nameof(Login));
        }

        var passwordResult = VerifyPassword(
            user,
            model.OldPassword);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(
                nameof(model.OldPassword),
                "Mật khẩu hiện tại không chính xác.");

            return View(
                "~/Views/Login/ChangePassword.cshtml",
                model);
        }

        user.Password = _passwordHasher.HashPassword(
            user,
            model.NewPassword);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đổi mật khẩu thành công.";

        var role = HttpContext.Session.GetString("Role");

        return RedirectByRole(role);
    }

    // ĐĂNG XUẤT BẰNG LINK GET
    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        TempData["Success"] =
            "Bạn đã đăng xuất.";

        return RedirectToAction(
            "Index",
            "Home");
    }

    // ĐĂNG XUẤT BẰNG FORM POST TRONG ADMIN
    [HttpPost]
    [ActionName("Logout")]
    [ValidateAntiForgeryToken]
    public IActionResult LogoutPost()
    {
        HttpContext.Session.Clear();

        TempData["Success"] =
            "Bạn đã đăng xuất.";

        return RedirectToAction(
            "Index",
            "Home");
    }
}