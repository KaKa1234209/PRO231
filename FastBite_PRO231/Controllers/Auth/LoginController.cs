using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers.Auth
    {
        public class LoginController : Controller
        {
            private readonly FastBiteDbContext _context;

            public LoginController(FastBiteDbContext context)
            {
                _context = context;
            }

            // ================= ĐĂNG NHẬP =================

            [HttpGet]
            public IActionResult Login()
            {
                return View();
            }

            [HttpPost]
            public IActionResult Login(LoginViewModel model)
            {
                if (!ModelState.IsValid)
                    return View(model);//Giữ lại dữ liệu đã nhập ko bị reset

                var user = _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.UserName == model.UserName);

                if (user == null)
                {
                    ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu");
                    return View(model);
                }


                if (user.Password != model.Password)
                {
                    ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu");
                    return View(model);
                }

                if (user.Status != "Hoạt động")
                {
                    ModelState.AddModelError("",
                        $"Tài khoản hiện đang ở trạng thái: {user.Status}");
                    return View(model);
                }
                //lưu thông tin sau khi đăng nhập thành công
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("Role", user.Role.RoleName);

                switch (user.Role.RoleName)
                {
                    case "Admin":
                        return RedirectToAction("Index", "AdminDashboard");

                    case "Employee":
                        return RedirectToAction("Index", "EmployeeDashboard");

                    case "Customer":
                        return RedirectToAction("Index", "CustomerDashboard");

                    default:
                        return RedirectToAction("Login");
                }
            }

            // ================= ĐĂNG KÝ =================

            [HttpGet]
            public IActionResult Register()
            {
                return View();
            }

            [HttpPost]
            public IActionResult Register(RegisterViewModel model)
            {
                if (!ModelState.IsValid)
                    return View(model);

                if (_context.Users.Any(x => x.UserName == model.UserName))
                {
                    ModelState.AddModelError("UserName",
                        "Tên đăng nhập đã tồn tại");
                }

                if (_context.Users.Any(x => x.Email == model.Email))
                {
                    ModelState.AddModelError("Email",
                        "Email đã tồn tại");
                }

                if (_context.Users.Any(x => x.Phone == model.Phone))
                {
                    ModelState.AddModelError("Phone",
                        "Số điện thoại đã tồn tại");
                }

                if (!ModelState.IsValid)
                    return View(model);

                User user = new User
                {
                    UserName = model.UserName,
                    Password = model.Password,
                    Email = model.Email,
                    Phone = model.Phone,
                    Status = "Hoạt động",
                    CreatedAt = DateTime.Now,
                    RoleId = 3 // Customer
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                Customer customer = new Customer
                {
                    UserId = user.UserId,
                    FullName = model.UserName,
                    Address = "",
                    Point = 0
                };

                _context.Customers.Add(customer);
                _context.SaveChanges();

                TempData["Success"] = "Đăng ký thành công";

                return RedirectToAction("Login");
            }

            // ================= ĐỔI MẬT KHẨU =================

            [HttpGet]
            public IActionResult ChangePassword()
            {
                return View();
            }

            [HttpPost]
            public IActionResult ChangePassword(ChangePasswordViewModel model)
            {
                if (!ModelState.IsValid)
                    return View(model);

                int? userId = HttpContext.Session.GetInt32("UserId");

                if (userId == null)
                    return RedirectToAction("Login");

                var user = _context.Users.Find(userId);

                if (user == null)
                    return RedirectToAction("Login");

                if (user.Password != model.OldPassword)
                {
                    ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng");
                    return View(model);
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp");
                    return View(model);
                }

                user.Password = model.NewPassword;
                _context.SaveChanges();

                TempData["Success"] = "Đổi mật khẩu thành công";

                return RedirectToAction("Login");
            }

            // ================= QUÊN MẬT KHẨU =================

            [HttpGet]
            public IActionResult ForgotPassword()
            {
                return View();
            }

            [HttpPost]
            public IActionResult ForgotPassword(ForgotPasswordViewModel model)
            {
                if (!ModelState.IsValid)
                    return View(model);

                var user = _context.Users.FirstOrDefault(x => x.Email == model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("Email", "Email không tồn tại");
                    return View(model);
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Mật khẩu không khớp");
                    return View(model);
                }

                // KHÔNG nên set thẳng
                user.Password = model.NewPassword;
                _context.SaveChanges();

                TempData["Success"] = "Đặt lại mật khẩu thành công";

                return RedirectToAction("Login");
            }

            // ================= ĐĂNG XUẤT =================

            public IActionResult Logout()
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }
        }
    }