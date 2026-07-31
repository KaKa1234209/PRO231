using System;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

public class CustomerHomeController : Controller
{
    private readonly FastBiteDbContext _context;
    public CustomerHomeController(
        FastBiteDbContext context)
    {
        _context = context;
    }

    private bool IsCustomer()
    {
        var role =
            HttpContext.Session.GetString("Role");

        return string.Equals(
            role,
            "Customer",
            StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectUnauthorized()
    {
        TempData["Error"] =
            "Vui lòng đăng nhập bằng tài khoản khách hàng.";

        return RedirectToAction(
            "Login",
            "Login");
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsCustomer())
        {
            return RedirectUnauthorized();
        }

        var userId =
            HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue)
        {
            return RedirectUnauthorized();
        }

        var customer =
            await _context.Customers
                .AsNoTracking()
                .Include(item => item.User)
                .Include(item => item.Cart)
                    .ThenInclude(cart => cart!.CartItems)
                .FirstOrDefaultAsync(item =>
                    item.UserId == userId.Value);

        if (customer == null)
        {
            HttpContext.Session.Clear();

            TempData["Error"] =
                "Không tìm thấy hồ sơ khách hàng.";

            return RedirectToAction(
                "Login",
                "Login");
        }

        if (!string.Equals(
                customer.User.Status,
                "Hoạt động",
                StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.Clear();

            TempData["Error"] =
                "Tài khoản khách hàng đã bị ngừng hoạt động.";

            return RedirectToAction(
                "Login",
                "Login");
        }

        var orders =
            await _context.Orders
                .AsNoTracking()
                .Include(order =>
                    order.OrderDetails)
                .Include(order =>
                    order.Invoices)
                .Where(order =>
                    order.CustomerId ==
                    customer.CustomerId)
                .OrderByDescending(order =>
                    order.OrderDate)
                .ThenByDescending(order =>
                    order.OrderId)
                .ToListAsync();

        var totalSpent =
            await _context.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    invoice.Status &&
                    invoice.Order.CustomerId ==
                    customer.CustomerId)
                .SumAsync(invoice =>
                    (decimal?)invoice.TotalAmount)
            ?? 0m;

        var promotionEntities =
            await _context.Promotions
                .AsNoTracking()
                .Include(promotion =>
                    promotion.PromotionDetails)
                    .ThenInclude(detail =>
                        detail.Product)
                .Where(promotion =>
                    promotion.Status ==
                    "Đang hoạt động")
                .OrderByDescending(promotion =>
                    promotion.PromotionId)
                .Take(4)
                .ToListAsync();


        var model =
            new CustomerDashboardViewModel
            {
                CustomerId = customer.CustomerId, 
                UserId = customer.UserId, 
                FullName = customer.User.FullName, 
                UserName = customer.User.UserName, 
                Email = customer.User.Email, 
                Phone = customer.User.Phone, 
                Address = customer.Address ?? "", 
                Point = customer.Point,

                CartItemCount =
                    customer.Cart?.CartItems
                        .Sum(item => item.Quantity) ?? 0,

                TotalOrders = orders.Count,

                PendingOrders = orders.Count(order =>
                            PendingStatuses.Contains(
                            order.Status)),

                ProcessingOrders =
                    orders.Count(order =>
                        ProcessingStatuses.Contains(
                            order.Status)),

                CompletedOrders =
                    orders.Count(order =>
                        CompletedStatuses.Contains(
                            order.Status)),

                CancelledOrders =
                    orders.Count(order =>
                        string.Equals(
                            order.Status,
                            "Đã hủy",
                            StringComparison.OrdinalIgnoreCase)),

                TotalSpent = totalSpent,

                RecentOrders = orders
                        .Take(6)
                        .Select(order =>
                            new CustomerDashboardOrderViewModel
                            {
                                OrderId = order.OrderId, 
                                OrderDate = order.OrderDate, 
                                TotalAmount = order.TotalAmount,
                                Status = order.Status,
                                TotalQuantity =order.OrderDetails.Sum(detail => detail.Quantity),
                                HasInvoice = order.Invoices.Any()
                            })
                        .ToList(),

                Promotions = promotionEntities
                        .Select(promotion =>
                        {
                            var names = promotion.PromotionDetails
                                    .Select(detail => detail.Product.ProductName)
                                    .Take(3)
                                    .ToList();

                            var remaining = promotion.PromotionDetails.Count - names.Count;

                            if (remaining > 0)
                            {
                                names.Add($"+{remaining} món khác");
                            }

                            return new
                                CustomerDashboardPromotionViewModel
                            {
                                PromotionId = promotion.PromotionId, 
                                PromotionName = promotion.PromotionName, 
                                DiscountType = promotion.DiscountType, 
                                DiscountValue = promotion.DiscountValue, 
                                ProductCount = promotion.PromotionDetails.Count,
                                ProductNames = names.Count == 0 ? "Chưa chọn sản phẩm" : string.Join(", ", names)
                            };
                        })
                        .ToList()
            };

        return View(
            "~/Views/CustomerHome/Index.cshtml",
            model);
    }
}