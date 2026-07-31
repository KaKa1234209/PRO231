using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderModel = FastBite_PRO231.Models.Order;

namespace FastBite_PRO231.Controllers;

public class CheckoutController : Controller
{
    private readonly FastBiteDbContext _context;

    public CheckoutController(FastBiteDbContext context)
    {
        _context = context;
    }
    //kiểm tra người dùng
    private bool IsCustomer()
    {
        var role = HttpContext.Session.GetString("Role");
        return string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase);
    }

    //Chuẩn hóa đường dẫn ảnh
    private static string NormalizeImageUrl(string? image)
    {
        if (string.IsNullOrWhiteSpace(image)) return "";
        image = image.Trim();

        if (image.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            image.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return image;

        if (image.StartsWith("~/")) return image[1..];
        if (image.StartsWith("/")) return image;

        return $"/images/products/{image}";
    }

    // TODO: THAY bằng toạ độ THẬT của quán FastBite (lấy từ Google Maps, click chuột phải vào vị trí quán)
    private const double StoreLatitude = 10.762622;
    private const double StoreLongitude = 106.660172;

    // Công thức Haversine - tính khoảng cách đường chim bay giữa 2 toạ độ (km)
    private static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371;

        double dLat = ToRadians(lat2 - lat1);
        double dLng = ToRadians(lng2 - lng1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    private static decimal CalculateDeliveryFee(decimal subtotal, double? lat, double? lng)
    {
        // Miễn phí ship nếu đơn đủ lớn, bất kể khoảng cách
        if (subtotal >= 200000m)
        {
            return 0m;
        }

        // Khách KHÔNG ghim vị trí (chỉ gõ địa chỉ tay) -> không tính được khoảng cách
        // -> dùng mức phí mặc định cố định, không bắt buộc phải ghim mới đặt được hàng
        if (!lat.HasValue || !lng.HasValue)
        {
            return 15000m;
        }

        var distanceKm = CalculateDistanceKm(StoreLatitude, StoreLongitude, lat.Value, lng.Value);

        return distanceKm switch
        {
            <= 3 => 15000m,
            <= 7 => 25000m,
            _ => 35000m
        };
    }

    private IActionResult RedirectToLogin()
    {
        TempData["Error"] = "Vui lòng đăng nhập bằng tài khoản khách hàng.";
        return RedirectToAction("Login", "Login");
    }

    // =========================================
    // TRANG XÁC NHẬN ĐƠN HÀNG
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsCustomer()) return RedirectToLogin();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToLogin();

        var customer = await _context.Customers
            .AsNoTracking()
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.UserId == userId.Value);

        if (customer == null)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ khách hàng.";
            return RedirectToAction("Index", "Cart");
        }

        var cart = await _context.Carts
            .AsNoTracking()
            .Include(item => item.CartItems)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.CustomerId == customer.CustomerId);

        if (cart == null || cart.CartItems.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        var items = cart.CartItems
            .Select(item => new CheckoutItemViewModel
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.ProductName ?? "Sản phẩm",
                ImageUrl = NormalizeImageUrl(item.Product?.Image),
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                SubTotal = item.Price * item.Quantity
            })
            .ToList();

        var subtotal = items.Sum(item => item.SubTotal);

        var model = new CheckoutViewModel
        {
            FullName = customer.User?.FullName ?? customer.User?.UserName ?? "Khách hàng",
            Phone = customer.User?.Phone ?? "",
            Email = customer.User?.Email ?? "",
            Address = customer.Address ?? "",

            TotalQuantity = items.Sum(item => item.Quantity),
            TotalAmount = items.Sum(item => item.SubTotal),

            PaymentMethod = "COD",
            Note = null,
            DeliveryFee = CalculateDeliveryFee(subtotal, null, null),

            Items = items
        };

        return View(model);
    }

    // =========================================
    // TẠO ĐƠN HÀNG
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        if (!IsCustomer()) return RedirectToLogin();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToLogin();

        var customer = await _context.Customers
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.UserId == userId.Value);

        if (customer == null)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ khách hàng.";
            return RedirectToAction("Index", "Cart");
        }

        var cart = await _context.Carts
            .Include(item => item.CartItems)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item => item.CustomerId == customer.CustomerId);

        if (cart == null || cart.CartItems.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        if (!ModelState.IsValid)
        {
            model.Items = cart.CartItems.Select(item => new CheckoutItemViewModel
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.ProductName ?? "Sản phẩm",
                ImageUrl = NormalizeImageUrl(item.Product?.Image),
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                SubTotal = item.Price * item.Quantity
            }).ToList();

            model.TotalQuantity = model.Items.Sum(item => item.Quantity);
            model.TotalAmount = model.Items.Sum(item => item.SubTotal);
            model.DeliveryFee = CalculateDeliveryFee(model.TotalAmount, model.Latitude, model.Longitude);

            return View("Index", model);
        }

        // Chỉ chấp nhận 2 giá trị hợp lệ, tránh khách sửa tay HTML gửi giá trị lạ
        if (model.PaymentMethod != "COD" && model.PaymentMethod != "VNPay")
        {
            TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
            return RedirectToAction("Index");
        }

        var productIds = cart.CartItems.Select(item => item.ProductId).Distinct().ToList();

        var inventories = await _context.Inventories
            .Where(item => productIds.Contains(item.ProductId))
            .ToListAsync();

        var inventoryDictionary = inventories.ToDictionary(item => item.ProductId);

        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product == null || !cartItem.Product.Status)
            {
                TempData["Error"] = "Có sản phẩm đã ngừng bán. Vui lòng kiểm tra lại giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }

            if (!inventoryDictionary.TryGetValue(cartItem.ProductId, out var inventory))
            {
                TempData["Error"] = $"Sản phẩm {cartItem.Product.ProductName} chưa có dữ liệu tồn kho.";
                return RedirectToAction("Index", "Cart");
            }

            if (inventory.Quantity < cartItem.Quantity)
            {
                TempData["Error"] = $"{cartItem.Product.ProductName} chỉ còn {inventory.Quantity} phần trong kho.";
                return RedirectToAction("Index", "Cart");
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            decimal totalAmount = 0;

            var order = new OrderModel
            {
                CustomerId = customer.CustomerId,
                EmployeeId = null,
                OrderDate = DateTime.Now,
                Status = "Đang chờ xử lý",
                TotalAmount = 0,

                DeliveryAddress = model.Address,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Note = model.Note,

                PaymentMethod = model.PaymentMethod,

                // COD: coi như "chưa thanh toán" cho tới khi giao hàng thu tiền xong
                // VNPay: cũng bắt đầu là "chưa thanh toán", chờ callback xác nhận mới đổi
                PaymentStatus = "Chưa thanh toán"
            };

            foreach (var cartItem in cart.CartItems)
            {
                var unitPrice = cartItem.Product?.Price ?? cartItem.Price;
                var subTotal = unitPrice * cartItem.Quantity;
                totalAmount += subTotal;

                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = unitPrice
                });

                var inventory = inventoryDictionary[cartItem.ProductId];
                inventory.Quantity -= cartItem.Quantity;
                inventory.UpdateAt = DateTime.Now;
            }
                
            var deliveryFee = CalculateDeliveryFee(totalAmount, model.Latitude, model.Longitude);
            order.DeliveryFee = deliveryFee;
            order.TotalAmount = totalAmount + deliveryFee;

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cart.CartItems);
            cart.TotalPrice = 0;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // ===== RẼ NHÁNH THEO PHƯƠNG THỨC THANH TOÁN =====

            if (model.PaymentMethod == "VNPay")
            {
                // Chưa xoá giỏ hàng khỏi transaction là ĐÚNG ở đây vì đơn đã tạo,
                // giờ chỉ cần đẩy khách sang cổng thanh toán VNPay.
                // Sẽ implement action tạo URL thanh toán ở bước tiếp theo.
                return RedirectToAction(
                    "PayWithVnpay",
                    "Payment",
                    new { orderId = order.OrderId });
            }

            // COD: hoàn tất luôn, không cần thanh toán online
            TempData["Success"] = "Đặt hàng thành công.";
            return RedirectToAction(nameof(Success), new { orderId = order.OrderId });
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Không thể tạo đơn hàng. Vui lòng thử lại.";
            return RedirectToAction("Index", "Cart");
        }
    }

    // =========================================
    // TRANG ĐẶT HÀNG THÀNH CÔNG
    [HttpGet]
    public async Task<IActionResult> Success(int orderId)
    {
        if (!IsCustomer()) return RedirectToLogin();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToLogin();

        var order = await _context.Orders
            .AsNoTracking()
            .Include(item => item.Customer)
                .ThenInclude(customer => customer.User)
            .Include(item => item.OrderDetails)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(item =>
                item.OrderId == orderId &&
                item.Customer.UserId == userId.Value);

        if (order == null) return NotFound();

        var items = order.OrderDetails
            .Select(item => new OrderSuccessItemViewModel
            {
                ProductName = item.Product?.ProductName ?? "Sản phẩm",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.UnitPrice * item.Quantity
            })
            .ToList();

        var model = new OrderSuccessViewModel
        {
            OrderId = order.OrderId,
            Latitude = order.Latitude,
            Longitude = order.Longitude,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            TotalQuantity = items.Sum(item => item.Quantity),

            FullName = order.Customer.User?.FullName ?? order.Customer.User?.UserName ?? "Khách hàng",
            Phone = order.Customer.User?.Phone ?? "",
            Address = order.DeliveryAddress ?? order.Customer.Address ?? "",

            Items = items
        };

        return View(model);
    }
}