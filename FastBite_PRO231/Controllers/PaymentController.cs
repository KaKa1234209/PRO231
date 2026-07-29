using FastBite_PRO231.Helpers;
using FastBite_PRO231.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

public class PaymentController : Controller
{
    private readonly FastBiteDbContext _context;
    private readonly IConfiguration _config;

    public PaymentController(FastBiteDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    private bool IsCustomer()
    {
        var role = HttpContext.Session.GetString("Role");
        return string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase);
    }

    // =========================================
    // TẠO URL THANH TOÁN, REDIRECT SANG VNPAY
    // GET: /Payment/PayWithVnpay?orderId=5
    // =========================================
    [HttpGet]
    public async Task<IActionResult> PayWithVnpay(int orderId)
    {
        if (!IsCustomer())
        {
            TempData["Error"] = "Vui lòng đăng nhập bằng tài khoản khách hàng.";
            return RedirectToAction("Login", "Login");
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Login");

        var order = await _context.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o =>
                o.OrderId == orderId &&
                o.Customer.UserId == userId.Value);

        if (order == null)
        {
            TempData["Error"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction("Index", "Cart");
        }

        if (order.PaymentMethod != "VNPay")
        {
            TempData["Error"] = "Đơn hàng này không sử dụng phương thức VNPay.";
            return RedirectToAction("Index", "Cart");
        }

        if (order.PaymentStatus == "Đã thanh toán")
        {
            TempData["Success"] = "Đơn hàng đã được thanh toán trước đó.";
            return RedirectToAction("Success", "Checkout", new { orderId = order.OrderId });
        }

        var vnpay = new VnpayLibrary();

        var tmnCode = _config["Vnpay:TmnCode"]!;
        var hashSecret = _config["Vnpay:HashSecret"]!;
        var baseUrl = _config["Vnpay:BaseUrl"]!;
        var returnUrl = _config["Vnpay:ReturnUrl"]!;

        // Số tiền VNPay yêu cầu nhân 100 (không có phần thập phân)
        var amount = ((long)order.TotalAmount * 100).ToString();

        vnpay.AddRequestData("vnp_Version", _config["Vnpay:Version"]!);
        vnpay.AddRequestData("vnp_Command", _config["Vnpay:Command"]!);
        vnpay.AddRequestData("vnp_TmnCode", tmnCode);
        vnpay.AddRequestData("vnp_Amount", amount);
        vnpay.AddRequestData("vnp_CurrCode", _config["Vnpay:CurrCode"]!);
        vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString());
        vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang FastBite {order.OrderId}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_Locale", _config["Vnpay:Locale"]!);
        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_IpAddr", GetClientIpAddress());
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        // Hết hạn thanh toán sau 15 phút
        vnpay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

        var paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);

        return Redirect(paymentUrl);
    }

    // =========================================
    // CALLBACK TỪ VNPAY SAU KHI THANH TOÁN
    // GET: /Payment/VnpayReturn
    // =========================================
    [HttpGet]
    public async Task<IActionResult> VnpayReturn()
    {
        var vnpay = new VnpayLibrary();

        foreach (var key in Request.Query.Keys)
        {
            if (key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, Request.Query[key].ToString());
            }
        }

        var secureHash = Request.Query["vnp_SecureHash"].ToString();
        var hashSecret = _config["Vnpay:HashSecret"]!;

        var isValidSignature = vnpay.ValidateSignature(secureHash, hashSecret);

        if (!isValidSignature)
        {
            TempData["Error"] = "Chữ ký không hợp lệ. Giao dịch có thể đã bị giả mạo.";
            return RedirectToAction("Index", "Cart");
        }

        var orderIdText = vnpay.GetResponseData("vnp_TxnRef");
        var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var transactionNo = vnpay.GetResponseData("vnp_TransactionNo");
        var transactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");

        if (!int.TryParse(orderIdText, out var orderId))
        {
            TempData["Error"] = "Không xác định được đơn hàng.";
            return RedirectToAction("Index", "Cart");
        }

        var order = await _context.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            TempData["Error"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction("Index", "Cart");
        }

        // Nếu đã xử lý callback trước đó (VNPay có thể gọi lại), tránh xử lý 2 lần
        if (order.PaymentStatus == "Đã thanh toán")
        {
            return RedirectToAction("Success", "Checkout", new { orderId = order.OrderId });
        }

        var isSuccess = responseCode == "00" && transactionStatus == "00";

        if (!isSuccess)
        {
            order.PaymentStatus = "Thanh toán thất bại";
            order.Status = "Đã huỷ";

            await _context.SaveChangesAsync();

            TempData["Error"] = "Thanh toán không thành công. Đơn hàng đã bị huỷ, vui lòng đặt lại.";
            return RedirectToAction("Index", "Cart");
        }

        // ===== THANH TOÁN THÀNH CÔNG: kiểm tra lại tồn kho LẦN 2 rồi mới trừ kho =====

        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.CustomerId == order.Customer.CustomerId);

        var orderDetails = await _context.OrderDetails
            .Where(od => od.OrderId == order.OrderId)
            .ToListAsync();

        var productIds = orderDetails.Select(od => od.ProductId).Distinct().ToList();

        var inventories = await _context.Inventories
            .Where(inv => productIds.Contains(inv.ProductId))
            .ToListAsync();

        var inventoryDictionary = inventories.ToDictionary(inv => inv.ProductId);

        var outOfStockItems = new List<string>();

        foreach (var detail in orderDetails)
        {
            if (!inventoryDictionary.TryGetValue(detail.ProductId, out var inventory) ||
                inventory.Quantity < detail.Quantity)
            {
                outOfStockItems.Add(detail.ProductId.ToString());
            }
        }

        if (outOfStockItems.Count > 0)
        {
            // Khách ĐÃ trả tiền nhưng hàng không đủ nữa
            // -> đánh dấu đặc biệt để Admin xử lý hoàn tiền thủ công, KHÔNG tự ý trừ kho
            order.PaymentStatus = "Đã thanh toán";
            order.TransactionId = transactionNo;
            order.PaidAt = DateTime.Now;
            order.Status = "Cần hoàn tiền"; // trạng thái đặc biệt cho Admin xử lý

            await _context.SaveChangesAsync();

            TempData["Error"] =
                "Bạn đã thanh toán thành công, nhưng một số món trong đơn vừa hết hàng. " +
                "Bộ phận CSKH sẽ liên hệ để hoàn tiền hoặc đổi món trong thời gian sớm nhất.";

            return RedirectToAction("Success", "Checkout", new { orderId = order.OrderId });
        }

        // Đủ hàng: trừ kho + xoá giỏ + xác nhận đơn
        foreach (var detail in orderDetails)
        {
            var inventory = inventoryDictionary[detail.ProductId];
            inventory.Quantity -= detail.Quantity;
            inventory.UpdateAt = DateTime.Now;
        }

        order.PaymentStatus = "Đã thanh toán";
        order.TransactionId = transactionNo;
        order.PaidAt = DateTime.Now;
        order.Status = "Đang chờ xử lý";

        if (cart != null && cart.CartItems.Count > 0)
        {
            _context.CartItems.RemoveRange(cart.CartItems);
            cart.TotalPrice = 0;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Thanh toán thành công! Đơn hàng của bạn đang được xử lý.";
        return RedirectToAction("Success", "Checkout", new { orderId = order.OrderId });
    }

    private string GetClientIpAddress()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        // Localhost IPv6 -> đổi về IPv4 để VNPay chấp nhận
        if (ip == "::1") ip = "127.0.0.1";

        return ip;
    }
}