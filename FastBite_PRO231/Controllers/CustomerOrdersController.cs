using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels.Customer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

[Route("customer/orders")]
public class CustomerOrdersController : Controller
{
    private readonly FastBiteDbContext _context;

    private static readonly HashSet<string> CancellableStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Đang chờ xử lý",
            "Chờ xử lý",
            "Chờ xác nhận"
        };

    public CustomerOrdersController(FastBiteDbContext context)
    {
        _context = context;
    }

    private bool IsCustomer()
    {
        var role = HttpContext.Session.GetString("Role");

        return string.Equals(
            role,
            "Customer",
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Customer?> GetCurrentCustomerAsync()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue)
        {
            return null;
        }

        return await _context.Customers
            .FirstOrDefaultAsync(customer =>
                customer.UserId == userId.Value);
    }

    private IActionResult RedirectToLogin()
    {
        TempData["Error"] =
            "Vui lòng đăng nhập bằng tài khoản khách hàng.";

        return RedirectToAction(
            "Login",
            "Login");
    }

    private static bool CanCancelOrder(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return CancellableStatuses.Contains(status.Trim());
    }

    private static string NormalizeImageUrl(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            return "";
        }

        image = image.Trim();

        if (image.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) ||
            image.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            return image;
        }

        if (image.StartsWith("~/"))
        {
            return image[1..];
        }

        if (image.StartsWith("/"))
        {
            return image;
        }

        return $"/images/products/{image}";
    }

    // =====================================================
    // DANH SÁCH ĐƠN HÀNG CỦA KHÁCH HÀNG
    // GET: /customer/orders
    // =====================================================

    [HttpGet("")]
    public async Task<IActionResult> Index(string? status)
    {
        if (!IsCustomer())
        {
            return RedirectToLogin();
        }

        var customer = await GetCurrentCustomerAsync();

        if (customer == null)
        {
            TempData["Error"] =
                "Không tìm thấy hồ sơ khách hàng.";

            return RedirectToLogin();
        }

        status = status?.Trim() ?? "";

        var query = _context.Orders
            .AsNoTracking()
            .Include(order => order.OrderDetails)
            .Where(order =>
                order.CustomerId == customer.CustomerId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(order =>
                order.Status == status);
        }

        var orderEntities = await query
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.OrderId)
            .ToListAsync();

        var model = new CustomerOrderHistoryViewModel
        {
            StatusFilter = status,

            Orders = orderEntities
                .Select(order =>
                    new CustomerOrderListItemViewModel
                    {
                        OrderId = order.OrderId,
                        OrderDate = order.OrderDate,
                        Status = order.Status,
                        TotalAmount = order.TotalAmount,

                        TotalQuantity =
                            order.OrderDetails.Sum(detail =>
                                detail.Quantity),

                        CanCancel =
                            CanCancelOrder(order.Status)
                    })
                .ToList()
        };

        return View(model);
    }

    // =====================================================
    // CHI TIẾT ĐƠN HÀNG
    // GET: /customer/orders/5
    // =====================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsCustomer())
        {
            return RedirectToLogin();
        }

        var customer = await GetCurrentCustomerAsync();

        if (customer == null)
        {
            TempData["Error"] =
                "Không tìm thấy hồ sơ khách hàng.";

            return RedirectToLogin();
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Include(item => item.OrderDetails)
                .ThenInclude(detail => detail.Product)
            .FirstOrDefaultAsync(item =>
                item.OrderId == id &&
                item.CustomerId == customer.CustomerId);

        if (order == null)
        {
            return NotFound();
        }

        var items = order.OrderDetails
            .Select(detail =>
                new CustomerOrderDetailItemViewModel
                {
                    ProductId = detail.ProductId,

                    ProductName =
                        detail.Product?.ProductName
                        ?? "Sản phẩm",

                    ImageUrl =
                        NormalizeImageUrl(
                            detail.Product?.Image),

                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,

                    SubTotal =
                        detail.UnitPrice *
                        detail.Quantity
                })
            .ToList();

        var model = new CustomerOrderDetailsViewModel
        {
            OrderId = order.OrderId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,

            TotalQuantity = items.Sum(item =>
                item.Quantity),

            CanCancel =
                CanCancelOrder(order.Status),

            Items = items
        };

        return View(model);
    }

    // =====================================================
    // HỦY ĐƠN HÀNG VÀ HOÀN LẠI TỒN KHO
    // POST: /customer/orders/5/cancel
    // =====================================================

    [HttpPost("{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!IsCustomer())
        {
            return RedirectToLogin();
        }

        var customer = await GetCurrentCustomerAsync();

        if (customer == null)
        {
            TempData["Error"] =
                "Không tìm thấy hồ sơ khách hàng.";

            return RedirectToLogin();
        }

        var order = await _context.Orders
            .Include(item => item.OrderDetails)
            .Include(item => item.Invoices)
            .FirstOrDefaultAsync(item =>
                item.OrderId == id &&
                item.CustomerId == customer.CustomerId);

        if (order == null)
        {
            TempData["Error"] =
                "Không tìm thấy đơn hàng.";

            return RedirectToAction(nameof(Index));
        }

        if (!CanCancelOrder(order.Status))
        {
            TempData["Error"] =
                "Đơn hàng đã được xử lý nên không thể hủy.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        if (order.Invoices.Any())
        {
            TempData["Error"] =
                "Đơn hàng đã có hóa đơn nên không thể hủy.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var detail in order.OrderDetails)
            {
                var inventory =
                    await _context.Inventories
                        .FirstOrDefaultAsync(item =>
                            item.ProductId ==
                            detail.ProductId);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        Unit = "Phần",
                        UpdateAt = DateTime.Now
                    };

                    _context.Inventories.Add(inventory);
                }
                else
                {
                    inventory.Quantity += detail.Quantity;
                    inventory.UpdateAt = DateTime.Now;
                }
            }

            order.Status = "Đã hủy";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                $"Đã hủy đơn hàng #{order.OrderId}. " +
                "Số lượng sản phẩm đã được hoàn lại kho.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            TempData["Error"] =
                "Không thể hủy đơn hàng. Vui lòng thử lại.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}