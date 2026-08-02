namespace FastBite_PRO231.Common;

public static class OrderStatusConstants
{
    // ===== Trạng thái đơn hàng =====
    public const string Pending = "Đang chờ xử lý";
    public const string Processing = "Đang xử lý";
    public const string Cancelled = "Đã hủy";
    public const string Completed = "Hoàn thành";
    public const string RefundNeeded = "Cần hoàn tiền";

    public static readonly string[] PendingStatuses = { Pending };

    public static readonly string[] ProcessingStatuses =
    {
        Processing,
        "Đang chuẩn bị",
        "Đang giao"
    };

    public static readonly string[] CompletedStatuses = { Completed, "Completed" };

    public static readonly string[] AllowedOrderStatuses =
    {
        Pending, Processing, "Đang chuẩn bị", "Đang giao", Completed, Cancelled
    };

    public static readonly string[] CancellableStatuses = { Pending };

    public static bool IsActiveOrder(string? status)
        => !string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);

    // ===== Nhân viên / Shipper (Employee.Status, Shipper.Status) =====
    public const string StaffWorking = "Đang làm việc";
    public const string StaffResigned = "Đã nghỉ việc";
    public static readonly string[] ValidStaffStatuses = { StaffWorking, StaffResigned };

    // ===== Tài khoản (User.Status) =====
    public const string AccountActive = "Hoạt động";
    public const string AccountInactive = "Ngừng hoạt động";
    public static readonly string[] ValidAccountStatuses = { AccountActive, AccountInactive };

    // ===== Thanh toán / đối soát =====
    public const string PaymentMethodCod = "COD";
    public const string PaymentMethodVnpay = "VNPay";

    public const string PaymentStatusUnpaid = "Chưa thanh toán";
    public const string PaymentStatusPaid = "Đã thanh toán";
    public const string PaymentStatusFailed = "Thanh toán thất bại";

    public const string SettlementPending = "Chưa đối soát";
    public const string SettlementDone = "Đã đối soát";

    // ===== Hóa đơn: phương thức thanh toán =====
    public static readonly string[] ValidInvoicePaymentMethods = { "Cash", "Banking", "Momo" };

    // ===== MỚI: Khuyến mãi (Promotion.Status) =====
    public const string PromotionActive = "Đang hoạt động";
    public const string PromotionPaused = "Tạm ngưng";
    public const string PromotionUpcoming = "Sắp diễn ra";

    public static readonly string[] ValidPromotionStatuses =
    {
        PromotionActive,
        PromotionPaused,
        PromotionUpcoming
    };

    // ===== MỚI: Khuyến mãi - loại giảm giá =====
    public const string DiscountTypePercent = "Percent";
    public const string DiscountTypeFixed = "Fixed";

    public static readonly string[] ValidDiscountTypes =
    {
        DiscountTypePercent,
        DiscountTypeFixed
    };
}