using System;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers.Admin;

public class CustomersController : Controller
{
    private readonly FastBiteDbContext _context;

    private static readonly string[] ValidStatuses =
    {
        "Hoạt động",
        "Ngừng hoạt động"
    };

    public CustomersController(FastBiteDbContext context)
    {
        _context = context;
    }


    // ==============================
    // CHECK ADMIN
    // ==============================

    private bool IsAdmin()
    {
        var role = HttpContext.Session.GetString("Role");

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
            "Chỉ tài khoản Admin mới được quản lý khách hàng.";

        return RedirectToAction(
            "Index",
            "Home");
    }


    private static bool IsCompletedStatus(string? status)
    {
        return string.Equals(
            status,
            "Hoàn thành",
            StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
            status,
            "Completed",
            StringComparison.OrdinalIgnoreCase);
    }


    private static bool IsCancelledStatus(string? status)
    {
        return string.Equals(
            status,
            "Đã hủy",
            StringComparison.OrdinalIgnoreCase);
    }



    // ==============================
    // INDEX
    // ==============================

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
            status?.Trim()
            .ToLowerInvariant()
            ?? "";


        var query =
            _context.Customers
            .AsNoTracking()
            .Include(c => c.User)
            .AsQueryable();



        if (!string.IsNullOrWhiteSpace(search))
        {
            query =
                query.Where(c =>
                    c.User.FullName.Contains(search)
                    ||
                    c.User.FullName.Contains(search)
                    ||
                    c.User.UserName.Contains(search)
                    ||
                    c.User.Email.Contains(search)
                    ||
                    c.User.Phone.Contains(search)
                    ||
                    c.Address.Contains(search));
        }



        if (status == "active")
        {
            query =
                query.Where(c =>
                    c.User.Status == "Hoạt động");
        }
        else if (status == "inactive")
        {
            query =
                query.Where(c =>
                    c.User.Status != "Hoạt động");
        }



        var customers =
            await query
            .OrderByDescending(c => c.CustomerId)
            .ToListAsync();



        var ids =
            customers
            .Select(c => c.CustomerId)
            .ToList();



        var orders =
            await _context.Orders
            .Where(o =>
                ids.Contains(o.CustomerId))
            .GroupBy(o =>
                o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count()
            })
            .ToListAsync();



        var spending =
            await _context.Invoices
            .Where(i =>
                i.Status
                &&
                ids.Contains(
                    i.Order.CustomerId))
            .GroupBy(i =>
                i.Order.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,

                Total =
                    g.Sum(x =>
                        x.TotalAmount)
            })
            .ToListAsync();



        var model =
            new CustomerManagementIndexViewModel
            {
                Search = search,

                StatusFilter = status,


                TotalCustomers =
                    await _context.Customers
                    .CountAsync(),


                ActiveCustomers =
                    await _context.Customers
                    .CountAsync(c =>
                        c.User.Status ==
                        "Hoạt động"),


                InactiveCustomers =
                    await _context.Customers
                    .CountAsync(c =>
                        c.User.Status !=
                        "Hoạt động"),



                CustomersWithOrders =
                    await _context.Orders
                    .Select(o =>
                        o.CustomerId)
                    .Distinct()
                    .CountAsync(),



                TotalPoints =
                    await _context.Customers
                    .SumAsync(c =>
                        (int?)c.Point)
                    ?? 0,



                Customers =
                    customers.Select(c =>
                    {
                        var order =
                            orders.FirstOrDefault(x =>
                                x.CustomerId ==
                                c.CustomerId);


                        var money =
                            spending.FirstOrDefault(x =>
                                x.CustomerId ==
                                c.CustomerId);



                        return new CustomerManagementItemViewModel
                        {
                            CustomerId =
                                c.CustomerId,

                            UserId =
                                c.UserId,


                            FullName = c.User.FullName,


                            UserName =
                                c.User.UserName,


                            Email =
                                c.User.Email,


                            Phone =
                                c.User.Phone,


                            Address =
                                c.Address,


                            Point =
                                c.Point,


                            Status =
                                c.User.Status,


                            CreatedAt =
                                c.User.CreatedAt,


                            OrderCount =
                                order?.Count ?? 0,


                            TotalSpent =
                                money?.Total ?? 0
                        };

                    }).ToList()
            };


        return View(
            "~/Views/Admin/Customers/Index.cshtml",
            model);
    }

    // ==============================
    // DETAILS
    // ==============================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }


        var customer =
            await _context.Customers
            .AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c =>
                c.CustomerId == id);


        if (customer == null)
        {
            return NotFound();
        }



        var orderEntities =
            await _context.Orders
            .AsNoTracking()
            .Include(o => o.Invoices)
            .Where(o =>
                o.CustomerId == id)
            .OrderByDescending(o =>
                o.OrderDate)
            .ToListAsync();



        var totalSpent =
            await _context.Invoices
            .AsNoTracking()
            .Where(i =>
                i.Status
                &&
                i.Order.CustomerId == id)
            .SumAsync(i =>
                (decimal?)i.TotalAmount)
            ?? 0m;



        var model =
            new CustomerManagementDetailsViewModel
            {
                CustomerId =
                    customer.CustomerId,

                UserId =
                    customer.UserId,


                FullName = customer.User.FullName,


                UserName =
                    customer.User.UserName,


                Email =
                    customer.User.Email,


                Phone =
                    customer.User.Phone,


                Address =
                    customer.Address,


                Point =
                    customer.Point,


                Status =
                    customer.User.Status,


                CreatedAt =
                    customer.User.CreatedAt,


                TotalOrders =
                    orderEntities.Count,


                CompletedOrders =
                    orderEntities.Count(o =>
                        IsCompletedStatus(
                            o.Status)),


                CancelledOrders =
                    orderEntities.Count(o =>
                        IsCancelledStatus(
                            o.Status)),


                TotalSpent =
                    totalSpent,


                RecentOrders =
                    orderEntities
                    .Take(10)
                    .Select(o =>
                        new CustomerOrderSummaryViewModel
                        {
                            OrderId =
                                o.OrderId,


                            OrderDate =
                                o.OrderDate,


                            TotalAmount =
                                o.TotalAmount,


                            Status =
                                o.Status,


                            HasInvoice =
                                o.Invoices.Any()
                        })
                    .ToList()
            };


        return View(
            "~/Views/Admin/Customers/Details.cshtml",
            model);
    }





    // ==============================
    // EDIT GET
    // ==============================

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }


        var customer =
            await _context.Customers
            .AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c =>
                c.CustomerId == id);



        if (customer == null)
        {
            return NotFound();
        }



        var model =
            new CustomerManagementEditViewModel
            {
                CustomerId =
                    customer.CustomerId,

                UserId =
                    customer.UserId,


                FullName = customer.User.FullName,

                UserName =
                    customer.User.UserName,


                Email =
                    customer.User.Email,


                Phone =
                    customer.User.Phone,


                Address =
                    customer.Address,


                Point =
                    customer.Point,


                Status =
                    customer.User.Status
            };


        return View(
            "~/Views/Admin/Customers/Edit.cshtml",
            model);
    }





    // ==============================
    // EDIT POST
    // ==============================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CustomerManagementEditViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }



        if (id != model.CustomerId)
        {
            return NotFound();
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


        model.Status =
            model.Status?.Trim() ?? "";



        var customer =
            await _context.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c =>
                c.CustomerId == id);



        if (customer == null)
        {
            return NotFound();
        }



        if (!ValidStatuses.Contains(
            model.Status,
            StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.Status),
                "Trạng thái không hợp lệ.");
        }



        var duplicateUser =
            await _context.Users.AnyAsync(u =>
                u.UserName == model.UserName
                &&
                u.UserId != customer.UserId);



        if (duplicateUser)
        {
            ModelState.AddModelError(
                nameof(model.UserName),
                "Tên đăng nhập đã tồn tại.");
        }



        var duplicateEmail =
            await _context.Users.AnyAsync(u =>
                u.Email == model.Email
                &&
                u.UserId != customer.UserId);



        if (duplicateEmail)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email đã tồn tại.");
        }



        var duplicatePhone =
            await _context.Users.AnyAsync(u =>
                u.Phone == model.Phone
                &&
                u.UserId != customer.UserId);



        if (duplicatePhone)
        {
            ModelState.AddModelError(
                nameof(model.Phone),
                "Số điện thoại đã tồn tại.");
        }



        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Admin/Customers/Edit.cshtml",
                model);
        }



        customer.Address =
            model.Address;


        customer.Point =
            model.Point;

        customer.User.FullName =
            model.FullName;


        customer.User.UserName =
            model.UserName;

        customer.User.Email =
            model.Email;

        customer.User.Phone =
            model.Phone;
        customer.User.Status =
            model.Status;



        await _context.SaveChangesAsync();



        TempData["Success"] =
            "Cập nhật khách hàng thành công.";



        return RedirectToAction(
            nameof(Details),
            new
            {
                id = customer.CustomerId
            });
    }





    // ==============================
    // ACTIVE / INACTIVE
    // ==============================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }



        var customer =
            await _context.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c =>
                c.CustomerId == id);



        if (customer == null)
        {
            return NotFound();
        }

        customer.User.Status =
            customer.User.Status ==
            "Hoạt động"
            ?
            "Ngừng hoạt động"
            :
            "Hoạt động";

        await _context.SaveChangesAsync();

        TempData["Success"] =
            customer.User.Status ==
            "Hoạt động"
            ?
            "Đã mở khóa tài khoản."
            :
            "Đã khóa tài khoản.";



        return RedirectToAction(
            nameof(Index));
    }
}