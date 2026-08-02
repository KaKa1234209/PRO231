using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Common;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers.Admin;

public class PromotionsController : Controller
{
    private readonly FastBiteDbContext _context;

    // ĐÃ XOÁ: ValidDiscountTypes và ValidStatuses local
    // -> dùng chung OrderStatusConstants.ValidDiscountTypes / ValidPromotionStatuses

    public PromotionsController(FastBiteDbContext context)
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
            TempData["Error"] = "Vui lòng đăng nhập trước.";
            return RedirectToAction("Login", "Login");
        }

        TempData["Error"] = "Chỉ tài khoản Admin mới được quản lý khuyến mãi.";
        return RedirectToAction("Index", "Home");
    }

    // =========================================
    // XỬ LÝ ĐƯỜNG DẪN ẢNH
    // =========================================

    private static string NormalizeImageUrl(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            return "";
        }

        image = image.Trim();

        if (image.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            image.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
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

    // =========================================
    // NẠP DANH SÁCH SẢN PHẨM CHO FORM
    // =========================================

    private async Task LoadProductsAsync(PromotionManagementFormViewModel model)
    {
        model.Products = await _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderBy(product => product.Category.CategoryName)
            .ThenBy(product => product.ProductName)
            .Select(product => new PromotionProductChoiceViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                CategoryName = product.Category == null ? "Chưa phân loại" : product.Category.CategoryName,
                Price = product.Price,
                ImageUrl = NormalizeImageUrl(product.Image),
                Status = product.Status
            })
            .ToListAsync();
    }

    // =========================================
    // KIỂM TRA DỮ LIỆU FORM
    // =========================================

    private async Task ValidateFormAsync(
        PromotionManagementFormViewModel model,
        int? currentPromotionId = null)
    {
        model.PromotionName = model.PromotionName?.Trim() ?? "";
        model.DiscountType = model.DiscountType?.Trim() ?? "";
        model.Status = model.Status?.Trim() ?? "";

        model.SelectedProductIds =
            (model.SelectedProductIds ?? new())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (!OrderStatusConstants.ValidDiscountTypes.Contains(
                model.DiscountType, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.DiscountType), "Loại giảm giá không hợp lệ.");
        }

        if (!OrderStatusConstants.ValidPromotionStatuses.Contains(
                model.Status, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Status), "Trạng thái khuyến mãi không hợp lệ.");
        }

        // Ngày kết thúc phải sau hoặc bằng ngày bắt đầu
        if (model.EndDate.Date < model.StartDate.Date)
        {
            ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
        }

        // Chặn giá trị âm cho DiscountValue
        if (model.DiscountValue < 0)
        {
            ModelState.AddModelError(nameof(model.DiscountValue), "Giá trị giảm giá không được âm.");
        }

        if (string.Equals(model.DiscountType, OrderStatusConstants.DiscountTypePercent, StringComparison.OrdinalIgnoreCase) &&
            model.DiscountValue > 100)
        {
            ModelState.AddModelError(nameof(model.DiscountValue), "Giảm theo phần trăm không được vượt quá 100%.");
        }

        if (model.SelectedProductIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.SelectedProductIds), "Vui lòng chọn ít nhất một sản phẩm.");
        }

        var duplicateName = await _context.Promotions
            .AsNoTracking()
            .AnyAsync(promotion =>
                promotion.PromotionName == model.PromotionName &&
                (!currentPromotionId.HasValue || promotion.PromotionId != currentPromotionId.Value));

        if (duplicateName)
        {
            ModelState.AddModelError(nameof(model.PromotionName), "Tên khuyến mãi này đã tồn tại.");
        }

        if (model.SelectedProductIds.Count > 0)
        {
            var validProductIds = await _context.Products
                .AsNoTracking()
                .Where(product => model.SelectedProductIds.Contains(product.ProductId))
                .Select(product => product.ProductId)
                .ToListAsync();

            if (validProductIds.Count != model.SelectedProductIds.Count)
            {
                ModelState.AddModelError(nameof(model.SelectedProductIds), "Có sản phẩm được chọn không tồn tại.");
            }
        }
    }

    // =========================================
    // DANH SÁCH KHUYẾN MÃI — GET: /Promotions
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        search = search?.Trim() ?? "";
        status = status?.Trim().ToLowerInvariant() ?? "";

        var query = _context.Promotions
            .AsNoTracking()
            .Include(promotion => promotion.PromotionDetails)
                .ThenInclude(detail => detail.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(promotion =>
                promotion.PromotionName.Contains(search) ||
                promotion.PromotionDetails.Any(detail => detail.Product.ProductName.Contains(search)));
        }

        if (status == "active")
        {
            query = query.Where(promotion => promotion.Status == OrderStatusConstants.PromotionActive);
        }
        else if (status == "paused")
        {
            query = query.Where(promotion => promotion.Status == OrderStatusConstants.PromotionPaused);
        }
        else if (status == "upcoming")
        {
            query = query.Where(promotion => promotion.Status == OrderStatusConstants.PromotionUpcoming);
        }

        var promotionEntities = await query
            .OrderByDescending(promotion => promotion.PromotionId)
            .ToListAsync();

        var model = new PromotionManagementIndexViewModel
        {
            Search = search,
            StatusFilter = status,

            TotalPromotions = await _context.Promotions.CountAsync(),

            ActivePromotions = await _context.Promotions
                .CountAsync(promotion => promotion.Status == OrderStatusConstants.PromotionActive),

            PausedPromotions = await _context.Promotions
                .CountAsync(promotion => promotion.Status == OrderStatusConstants.PromotionPaused),

            UpcomingPromotions = await _context.Promotions
                .CountAsync(promotion => promotion.Status == OrderStatusConstants.PromotionUpcoming),

            Promotions = promotionEntities
                .Select(promotion =>
                {
                    var productNames = promotion.PromotionDetails
                        .Select(detail => detail.Product.ProductName)
                        .Take(3)
                        .ToList();

                    var remaining = promotion.PromotionDetails.Count - productNames.Count;

                    if (remaining > 0)
                    {
                        productNames.Add($"+{remaining} sản phẩm khác");
                    }

                    return new PromotionManagementItemViewModel
                    {
                        PromotionId = promotion.PromotionId,
                        PromotionName = promotion.PromotionName,
                        DiscountType = promotion.DiscountType,
                        DiscountValue = promotion.DiscountValue,
                        Status = promotion.Status,
                        ProductCount = promotion.PromotionDetails.Count,

                        ProductNames = productNames.Count == 0
                            ? "Chưa áp dụng sản phẩm"
                            : string.Join(", ", productNames)
                    };
                })
                .ToList()
        };

        return View("~/Views/Admin/Promotion/Index.cshtml", model);
    }

    // =========================================
    // CHI TIẾT KHUYẾN MÃI — GET: /Promotions/Details/5
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var promotion = await _context.Promotions
            .AsNoTracking()
            .Include(item => item.PromotionDetails)
                .ThenInclude(detail => detail.Product)
                    .ThenInclude(product => product.Category)
            .FirstOrDefaultAsync(item => item.PromotionId == id);

        if (promotion == null)
        {
            return NotFound();
        }

        var model = new PromotionManagementDetailsViewModel
        {
            PromotionId = promotion.PromotionId,
            PromotionName = promotion.PromotionName,
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            Status = promotion.Status,

            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,

            Products = promotion.PromotionDetails
                .Select(detail => new PromotionProductChoiceViewModel
                {
                    ProductId = detail.ProductId,
                    ProductName = detail.Product.ProductName,
                    CategoryName = detail.Product.Category == null ? "Chưa phân loại" : detail.Product.Category.CategoryName,
                    Price = detail.Product.Price,
                    ImageUrl = NormalizeImageUrl(detail.Product.Image),
                    Status = detail.Product.Status
                })
                .OrderBy(product => product.ProductName)
                .ToList()
        };

        return View("~/Views/Admin/Promotion/Details.cshtml", model);
    }

    // =========================================
    // FORM TẠO KHUYẾN MÃI — GET: /Promotions/Create
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var model = new PromotionManagementFormViewModel
        {
            DiscountType = OrderStatusConstants.DiscountTypePercent,
            DiscountValue = 10,
            Status = OrderStatusConstants.PromotionActive,

            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7)
        };

        await LoadProductsAsync(model);

        return View("~/Views/Admin/Promotion/Create.cshtml", model);
    }

    // =========================================
    // LƯU KHUYẾN MÃI MỚI — POST: /Promotions/Create
    // =========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PromotionManagementFormViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        await ValidateFormAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadProductsAsync(model);
            return View("~/Views/Admin/Promotion/Create.cshtml", model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var promotion = new Promotion
            {
                PromotionName = model.PromotionName,
                DiscountType = model.DiscountType,
                DiscountValue = model.DiscountValue,
                Status = model.Status,

                // FIX chính: trước đây thiếu 2 dòng này -> lưu DB bị lỗi
                StartDate = model.StartDate,
                EndDate = model.EndDate
            };

            _context.Promotions.Add(promotion);

            await _context.SaveChangesAsync();

            var details = model.SelectedProductIds
                .Select(productId => new PromotionDetail
                {
                    PromotionId = promotion.PromotionId,
                    ProductId = productId
                })
                .ToList();

            _context.PromotionDetails.AddRange(details);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đã tạo khuyến mãi “{promotion.PromotionName}” thành công.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError("", "Không thể tạo khuyến mãi. Vui lòng thử lại.");

            await LoadProductsAsync(model);

            return View("~/Views/Admin/Promotion/Create.cshtml", model);
        }
    }

    // =========================================
    // FORM CHỈNH SỬA — GET: /Promotions/Edit/5
    // =========================================

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var promotion = await _context.Promotions
            .AsNoTracking()
            .Include(item => item.PromotionDetails)
            .FirstOrDefaultAsync(item => item.PromotionId == id);

        if (promotion == null)
        {
            return NotFound();
        }

        var model = new PromotionManagementFormViewModel
        {
            PromotionId = promotion.PromotionId,
            PromotionName = promotion.PromotionName,
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            Status = promotion.Status,

            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,

            SelectedProductIds = promotion.PromotionDetails
                .Select(detail => detail.ProductId)
                .ToList()
        };

        await LoadProductsAsync(model);

        return View("~/Views/Admin/Promotion/Edit.cshtml", model);
    }

    // =========================================
    // LƯU CHỈNH SỬA 
    // =========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PromotionManagementFormViewModel model)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        if (id != model.PromotionId)
        {
            return NotFound();
        }

        await ValidateFormAsync(model, id);

        if (!ModelState.IsValid)
        {
            await LoadProductsAsync(model);
            return View("~/Views/Admin/Promotion/Edit.cshtml", model);
        }

        var promotion = await _context.Promotions
            .Include(item => item.PromotionDetails)
            .FirstOrDefaultAsync(item => item.PromotionId == id);

        if (promotion == null)
        {
            return NotFound();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            promotion.PromotionName = model.PromotionName;
            promotion.DiscountType = model.DiscountType;
            promotion.DiscountValue = model.DiscountValue;
            promotion.Status = model.Status;

            promotion.StartDate = model.StartDate;
            promotion.EndDate = model.EndDate;

            _context.PromotionDetails.RemoveRange(promotion.PromotionDetails);

            var newDetails = model.SelectedProductIds
                .Select(productId => new PromotionDetail
                {
                    PromotionId = promotion.PromotionId,
                    ProductId = productId
                })
                .ToList();

            _context.PromotionDetails.AddRange(newDetails);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đã cập nhật khuyến mãi “{promotion.PromotionName}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError("", "Không thể cập nhật khuyến mãi.");

            await LoadProductsAsync(model);

            return View("~/Views/Admin/Promotion/Edit.cshtml", model);
        }
    }

    // =========================================
    // BẬT HOẶC TẠM NGƯNG 
    // =========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var promotion = await _context.Promotions
            .FirstOrDefaultAsync(item => item.PromotionId == id);

        if (promotion == null)
        {
            return NotFound();
        }

        // MỚI: chặn đổi trạng thái khuyến mãi đang "Sắp diễn ra" bằng nút này
        // (nút này chỉ dùng để đảo Đang hoạt động <-> Tạm ngưng)
        if (promotion.Status == OrderStatusConstants.PromotionUpcoming)
        {
            TempData["Error"] = "Khuyến mãi đang \"Sắp diễn ra\" — vui lòng vào Chỉnh sửa để đổi trạng thái.";
            return RedirectToAction(nameof(Index));
        }

        var isActive = string.Equals(
            promotion.Status,
            OrderStatusConstants.PromotionActive,
            StringComparison.OrdinalIgnoreCase);

        promotion.Status = isActive
            ? OrderStatusConstants.PromotionPaused
            : OrderStatusConstants.PromotionActive;

        await _context.SaveChangesAsync();

        TempData["Success"] = promotion.Status == OrderStatusConstants.PromotionActive
            ? $"Đã bật khuyến mãi “{promotion.PromotionName}”."
            : $"Đã tạm ngưng khuyến mãi “{promotion.PromotionName}”.";

        return RedirectToAction(nameof(Index));
    }

    // =========================================
    // XÓA KHUYẾN MÃI — POST: /Promotions/Delete
    // =========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var promotion = await _context.Promotions
            .Include(item => item.PromotionDetails)
            .FirstOrDefaultAsync(item => item.PromotionId == id);

        if (promotion == null)
        {
            return NotFound();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.PromotionDetails.RemoveRange(promotion.PromotionDetails);
            _context.Promotions.Remove(promotion);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đã xóa khuyến mãi “{promotion.PromotionName}”.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            TempData["Error"] = "Không thể xóa khuyến mãi này.";

            return RedirectToAction(nameof(Index));
        }
    }
}