using System;
using System.Linq;
using System.Threading.Tasks;
using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

public class InventoriesController : Controller
{
    private readonly FastBiteDbContext _context;

    public InventoriesController(
        FastBiteDbContext context)
    {
        _context = context;
    }

    // =========================================
    // KIỂM TRA QUYỀN ADMIN HOẶC NHÂN VIÊN
    private bool CanManageInvoices()
    {
        var role = HttpContext.Session.GetString("Role");
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectUnauthorized()
    {
        var userId =
            HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue)
        {
            TempData["Error"] = "Vui lòng đăng nhập để quản lý tồn kho.";

            return RedirectToAction("Login", "Login");
        }
        TempData["Error"] = "Bạn không có quyền quản lý tồn kho.";

        return RedirectToAction("Index", "Home");
    }

    // =========================================
    // CHUẨN HÓA ĐƯỜNG DẪN ẢNH
    private static string NormalizeImageUrl(
        string? image)
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

    // =========================================
    // XÁC ĐỊNH TRẠNG THÁI KHO
    private static string GetStockStatus(
        int quantity)
    {
        if (quantity <= 0)
        {
            return "Hết hàng";
        }

        if (quantity <= 10)
        {
            return "Sắp hết";
        }

        return "Còn hàng";
    }

    // =========================================
    // NẠP SẢN PHẨM CHƯA CÓ TỒN KHO
    private async Task LoadAvailableProductsAsync(
        InventoryManagementFormViewModel model,
        int? includeProductId = null)
    {
        var usedProductIds =
            _context.Inventories
                .AsNoTracking()
                .Select(inventory => inventory.ProductId);

        model.Products =
            await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Where(product =>
                    !usedProductIds.Contains(product.ProductId) ||
                    (
                        includeProductId.HasValue &&
                        product.ProductId == includeProductId.Value
                    ))
                .OrderBy(product =>
                    product.Category.CategoryName)
                .ThenBy(product =>
                    product.ProductName)
                .Select(product =>
                    new InventoryProductChoiceViewModel
                    {
                        ProductId = product.ProductId, 
                        ProductName = product.ProductName, 
                        CategoryName = product.Category.CategoryName, 
                        Price = product.Price, 
                        Status = product.Status
                    })
                .ToListAsync();
    }

    // =========================================
    // DANH SÁCH TỒN KHO
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? stock)
    {
        if (!CanManageInvoices())
        {
            return RedirectUnauthorized();
        }

        search = search?.Trim() ?? "";

        stock =
            stock?.Trim().ToLowerInvariant()
            ?? "";

        var query =
            _context.Inventories
                .AsNoTracking()
                .Include(inventory =>
                    inventory.Product)
                    .ThenInclude(product =>
                        product.Category)
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(inventory =>
                inventory.Product.ProductName
                    .Contains(search) ||
                inventory.Product.Category.CategoryName
                    .Contains(search) ||
                inventory.Unit.Contains(search));
        }

        if (stock == "instock")
        {
            query = query.Where(inventory =>
                inventory.Quantity > 10);
        }
        else if (stock == "low")
        {
            query = query.Where(inventory =>
                inventory.Quantity > 0 &&
                inventory.Quantity <= 10);
        }
        else if (stock == "out")
        {
            query = query.Where(inventory =>
                inventory.Quantity <= 0);
        }

        var inventoryEntities =
            await query
                .OrderBy(inventory =>
                    inventory.Quantity)
                .ThenBy(inventory =>
                    inventory.Product.ProductName)
                .ToListAsync();

        var totalQuantity =
            await _context.Inventories
                .AsNoTracking()
                .SumAsync(inventory =>
                    (int?)inventory.Quantity)
            ?? 0;

        var model =
            new InventoryManagementIndexViewModel
            {
                Search = search, 
                StockFilter = stock,

                TotalInventories =
                    await _context.Inventories
                        .AsNoTracking()
                        .CountAsync(),

                InStockProducts =
                    await _context.Inventories
                        .AsNoTracking()
                        .CountAsync(inventory =>
                            inventory.Quantity > 10),

                LowStockProducts =
                    await _context.Inventories
                        .AsNoTracking()
                        .CountAsync(inventory =>
                            inventory.Quantity > 0 &&
                            inventory.Quantity <= 10),

                OutOfStockProducts =
                    await _context.Inventories
                        .AsNoTracking()
                        .CountAsync(inventory =>
                            inventory.Quantity <= 0),

                TotalQuantity = totalQuantity,

                Inventories = inventoryEntities
                        .Select(inventory =>
                            new InventoryManagementItemViewModel
                            {
                                InventoryId =
                                    inventory.InventoryId,

                                ProductId =
                                    inventory.ProductId,

                                ProductName =
                                    inventory.Product.ProductName,

                                CategoryName =
                                    inventory.Product.Category
                                        .CategoryName,

                                ImageUrl =
                                    NormalizeImageUrl(
                                        inventory.Product.Image),

                                ProductStatus =
                                    inventory.Product.Status,

                                Quantity =
                                    inventory.Quantity,

                                Unit =
                                    inventory.Unit,

                                UpdateAt =
                                    inventory.UpdateAt,

                                StockStatus =
                                    GetStockStatus(
                                        inventory.Quantity)
                            })
                        .ToList()
            };

        return View(
            "~/Views/Admin/Inventory/Index.cshtml",
            model);
    }

    // =========================================
    // CHI TIẾT TỒN KHO
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!CanManageInvoices())
        {
            return RedirectUnauthorized();
        }

        var inventory =
            await _context.Inventories
                .AsNoTracking()
                .Include(item =>
                    item.Product)
                    .ThenInclude(product =>
                        product.Category)
                .FirstOrDefaultAsync(item =>
                    item.InventoryId == id);

        if (inventory == null)
        {
            return NotFound();
        }

        var model =
            new InventoryManagementDetailsViewModel
            {
                InventoryId =
                    inventory.InventoryId,

                ProductId =
                    inventory.ProductId,

                ProductName =
                    inventory.Product.ProductName,

                CategoryName =
                    inventory.Product.Category
                        .CategoryName,

                Description =
                    inventory.Product.Description
                    ?? "",

                ImageUrl =
                    NormalizeImageUrl(
                        inventory.Product.Image),

                Price =
                    inventory.Product.Price,

                ProductStatus =
                    inventory.Product.Status,

                Quantity =
                    inventory.Quantity,

                Unit =
                    inventory.Unit,

                UpdateAt =
                    inventory.UpdateAt,

                StockStatus =
                    GetStockStatus(
                        inventory.Quantity)
            };

        return View(
            "~/Views/Admin/Inventory/Details.cshtml",
            model);
    }

    // =========================================
    // FORM TẠO TỒN KHO
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!CanManageInvoices())
        {
            return RedirectUnauthorized();
        }

        var model =
            new InventoryManagementFormViewModel
            {
                Quantity =
                    0,

                Unit =
                    "Phần"
            };

        await LoadAvailableProductsAsync(model);

        return View(
            "~/Views/Admin/Inventory/Create.cshtml",
            model);
    }

    // =========================================
    // LƯU TỒN KHO MỚI
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        InventoryManagementFormViewModel model)
    {
        if (!CanManageInvoices())
        {
            return RedirectUnauthorized();
        }

        model.Unit =
            model.Unit?.Trim() ?? "";

        var productExists =
            await _context.Products
                .AsNoTracking()
                .AnyAsync(product =>
                    product.ProductId ==
                    model.ProductId);

        if (!productExists)
        {
            ModelState.AddModelError(
                nameof(model.ProductId),
                "Sản phẩm không tồn tại.");
        }

        var inventoryExists =
            await _context.Inventories
                .AsNoTracking()
                .AnyAsync(inventory =>
                    inventory.ProductId ==
                    model.ProductId);

        if (inventoryExists)
        {
            ModelState.AddModelError(
                nameof(model.ProductId),
                "Sản phẩm này đã có thông tin tồn kho.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAvailableProductsAsync(
                model,
                model.ProductId);

            return View(
                "~/Views/Admin/Inventory/Create.cshtml",
                model);
        }

        try
        {
            var inventory =
                new Inventory
                {
                    ProductId =
                        model.ProductId,

                    Quantity =
                        model.Quantity,

                    Unit =
                        model.Unit,

                    UpdateAt =
                        DateTime.Now
                };

            _context.Inventories.Add(inventory);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã tạo thông tin tồn kho thành công.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = inventory.InventoryId
                });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "Không thể tạo tồn kho. Sản phẩm có thể đã có bản ghi kho.");

            await LoadAvailableProductsAsync(
                model,
                model.ProductId);

            return View(
                "~/Views/Admin/Inventory/Create.cshtml",
                model);
        }
    }

    // =========================================
    // FORM CHỈNH SỬA 
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!CanManageInvoices())
        {
            return RedirectUnauthorized();
        }

        var inventory =
            await _context.Inventories
                .AsNoTracking()
                .Include(item =>
                    item.Product)
                .FirstOrDefaultAsync(item =>
                    item.InventoryId == id);

        if (inventory == null)
        {
            return NotFound();
        }

        var model =
            new InventoryManagementFormViewModel
            {
                InventoryId =
                    inventory.InventoryId,

                ProductId =
                    inventory.ProductId,

                ProductName =
                    inventory.Product.ProductName,

                Quantity =
                    inventory.Quantity,

                Unit =
                    inventory.Unit
            };

        await LoadAvailableProductsAsync(
            model,
            inventory.ProductId);

        return View(
            "~/Views/Admin/Inventory/Edit.cshtml",
            model);
    }

    // =========================================
    // LƯU CHỈNH SỬA
    // POST: /Inventories/Edit/5
    // =========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        InventoryManagementFormViewModel model)
    {
        if (!CanManageInvoices())
        {
            return RedirectUnauthorized();
        }

        if (id != model.InventoryId)
        {
            return NotFound();
        }

        model.Unit =
            model.Unit?.Trim() ?? "";

        var inventory =
            await _context.Inventories
                .Include(item =>
                    item.Product)
                .FirstOrDefaultAsync(item =>
                    item.InventoryId == id);

        if (inventory == null)
        {
            return NotFound();
        }

        // Không cho đổi sản phẩm của bản ghi tồn kho.
        model.ProductId =
            inventory.ProductId;

        model.ProductName =
            inventory.Product.ProductName;

        if (!ModelState.IsValid)
        {
            await LoadAvailableProductsAsync(
                model,
                inventory.ProductId);

            return View(
                "~/Views/Admin/Inventory/Edit.cshtml",
                model);
        }

        inventory.Quantity =
            model.Quantity;

        inventory.Unit =
            model.Unit;

        inventory.UpdateAt =
            DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Đã cập nhật tồn kho “{inventory.Product.ProductName}”.";

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = inventory.InventoryId
            });
    }

    // =========================================
    // NHẬP THÊM HOẶC XUẤT BỚT NHANH
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(
        int id,
        int change)
    {
        if (!CanManageInvoices())
        {
            return RedirectUnauthorized();
        }

        var inventory =
            await _context.Inventories
                .Include(item =>
                    item.Product)
                .FirstOrDefaultAsync(item =>
                    item.InventoryId == id);

        if (inventory == null)
        {
            return NotFound();
        }

        if (change == 0)
        {
            TempData["Error"] = "Số lượng thay đổi phải khác 0.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        if (change < -1000000 ||
            change > 1000000)
        {
            TempData["Error"] = "Số lượng thay đổi không hợp lệ.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var newQuantity =
            inventory.Quantity + change;

        if (newQuantity < 0)
        {
            TempData["Error"] =
                $"Không thể xuất {Math.Abs(change)} {inventory.Unit}. " +
                $"Kho chỉ còn {inventory.Quantity} {inventory.Unit}.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        inventory.Quantity =
            newQuantity;

        inventory.UpdateAt =
            DateTime.Now;

        await _context.SaveChangesAsync();

        if (change > 0)
        {
            TempData["Success"] =
                $"Đã nhập thêm {change} {inventory.Unit} " +
                $"cho “{inventory.Product.ProductName}”.";
        }
        else
        {
            TempData["Success"] =
                $"Đã xuất {Math.Abs(change)} {inventory.Unit} " +
                $"của “{inventory.Product.ProductName}”.";
        }

        return RedirectToAction(
            nameof(Details),
            new { id });
    }
}