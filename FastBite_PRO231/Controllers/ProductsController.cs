using FastBite_PRO231.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace FastBite_PRO231.Controllers;

public class ProductsController : Controller
{
    private readonly FastBiteDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private static readonly string[] AllowedImageExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB

    public ProductsController(
        FastBiteDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // =========================================
    // KIỂM TRA QUYỀN ADMIN
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
            TempData["Error"] = "Vui lòng đăng nhập tài khoản Admin.";
            return RedirectToAction("Login", "Login");
        }

        TempData["Error"] = "Chỉ tài khoản Admin mới được quản lý sản phẩm.";
        return RedirectToAction("Index", "Home");
    }

    private async Task LoadCategoriesAsync(int? selectedCategoryId = null)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.CategoryName)
            .ToListAsync();

        ViewBag.CategoryId = new SelectList(
            categories,
            "CategoryId",
            "CategoryName",
            selectedCategoryId);
    }

    private async Task ValidateProductAsync(Product product, int? currentProductId = null)
    {
        if (string.IsNullOrWhiteSpace(product.ProductName))
        {
            ModelState.AddModelError(
                nameof(product.ProductName),
                "Vui lòng nhập tên sản phẩm.");
        }

        if (product.CategoryId <= 0)
        {
            ModelState.AddModelError(
                nameof(product.CategoryId),
                "Vui lòng chọn danh mục.");
        }
        else
        {
            var categoryExists = await _context.Categories
                .AsNoTracking()
                .AnyAsync(category => category.CategoryId == product.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(product.CategoryId),
                    "Danh mục không tồn tại.");
            }
        }

        if (product.Price <= 0)
        {
            ModelState.AddModelError(
                nameof(product.Price),
                "Giá sản phẩm phải lớn hơn 0.");
        }

        if (!string.IsNullOrWhiteSpace(product.ProductName))
        {
            var nameExists = await _context.Products
                .AnyAsync(item =>
                    item.ProductName == product.ProductName &&
                    (!currentProductId.HasValue || item.ProductId != currentProductId.Value));

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(product.ProductName),
                    "Tên sản phẩm đã tồn tại.");
            }
        }
    }

    // =====================================================
    // VALIDATE LINK ẢNH
    // Chỉ chấp nhận URL tuyệt đối, scheme http/https.
    // (Trước đây dùng Uri.TryCreate(..., UriKind.Absolute, ...) đơn thuần
    //  sẽ vô tình chấp nhận cả javascript:, file:///, ftp://... -> không an toàn.)
    // =====================================================
    private bool TryValidateImageUrl(string? imageUrl, out string? cleanedUrl)
    {
        cleanedUrl = null;

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return true; // không nhập link thì coi như không có lỗi ở bước này
        }

        var trimmed = imageUrl.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        cleanedUrl = trimmed;
        return true;
    }

    // =====================================================
    // LƯU FILE ẢNH MỚI VÀO wwwroot/images/products
    // Trả về tên file đã lưu (null nếu không có ảnh hoặc lỗi).
    // =====================================================
    private async Task<string?> SaveImageFileAsync(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        if (!AllowedImageExtensions.Contains(extension))
        {
            ModelState.AddModelError(
                "Image",
                "Chỉ chấp nhận file ảnh JPG, PNG hoặc WEBP.");

            return null;
        }

        // Kiểm tra thêm Content-Type thực tế của file.
        // Chỉ xét đuôi file (.jpg, .png...) thì có thể bị đổi tên file giả mạo
        // (vd đổi virus.exe thành virus.jpg) để vượt qua kiểm tra.
        if (string.IsNullOrWhiteSpace(imageFile.ContentType) ||
            !imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                "Image",
                "File tải lên không phải là ảnh hợp lệ.");

            return null;
        }

        if (imageFile.Length > MaxImageSizeBytes)
        {
            ModelState.AddModelError(
                "Image",
                "Kích thước ảnh không được vượt quá 5MB.");

            return null;
        }

        var folder = Path.Combine(
            _environment.WebRootPath,
            "images",
            "products");

        Directory.CreateDirectory(folder);

        var fileName = Guid.NewGuid().ToString("N") + extension;
        var filePath = Path.Combine(folder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        return fileName;
    }

    // =====================================================
    // XÓA FILE ẢNH CŨ (khi thay ảnh mới hoặc xóa sản phẩm)
    // Không xóa nếu ảnh là link http/https (ảnh ngoài, không phải file local).
    private void DeleteImageFileIfExists(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        if (fileName.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var filePath = Path.Combine(
            _environment.WebRootPath,
            "images",
            "products",
            fileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    // Trang chủ: Danh sách + tìm kiếm
    public async Task<IActionResult> Index(string? searchString)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var query = _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();

            query = query.Where(product =>
                product.ProductName.Contains(searchString) ||
                (product.Description ?? "").Contains(searchString) ||
                (
                    product.Category != null &&
                    product.Category.CategoryName.Contains(searchString)
                ));
        }

        var products = await query
            .OrderBy(product => product.ProductId)
            .ToListAsync();

        ViewBag.SearchString = searchString;

        return View("~/Views/Admin/Product/Index.cshtml", products);
    }

    //Chi Tiết
    [HttpGet]
    public async Task<IActionResult> Details(int? productid)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        if (productid == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ProductId == productid.Value);

        if (product == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Product/Details.cshtml", product);
    }

    //Thêm
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        await LoadCategoriesAsync();

        var product = new Product
        {
            Price = 0,
            Status = true,
            Description = "",
            Image = ""
        };

        return View("~/Views/Admin/Product/Create.cshtml", product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    [Bind("CategoryId,ProductName,Price,Description,Status")]
    Product product,
    IFormFile? imageFile,
    string? imageUrl)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        product.ProductName = product.ProductName?.Trim() ?? "";
        product.Description = product.Description?.Trim() ?? "";

        ModelState.Remove(nameof(product.Image));
        ModelState.Remove(nameof(product.Category));

        // FIX: trước đây Create không kiểm tra tên/giá/category — giờ dùng chung
        // ValidateProductAsync với Edit để tránh tạo sản phẩm tên rỗng, giá <= 0,
        // category không tồn tại, hoặc trùng tên.
        await ValidateProductAsync(product);

        // Không được nhập cả 2
        if (imageFile != null &&
            !string.IsNullOrWhiteSpace(imageUrl))
        {
            ModelState.AddModelError(
                "Image",
                "Chỉ được chọn ảnh hoặc nhập link ảnh, không chọn cả hai.");
        }

        string? cleanedImageUrl = null;

        if (!string.IsNullOrWhiteSpace(imageUrl) &&
            !TryValidateImageUrl(imageUrl, out cleanedImageUrl))
        {
            ModelState.AddModelError(
                "Image",
                "Link ảnh không hợp lệ (phải bắt đầu bằng http:// hoặc https://).");
        }

        string? savedFileName = null;

        // chỉ lưu file khi các validate ở trên chưa có lỗi,
        // tránh trường hợp chọn cả file lẫn link -> vẫn ghi file ra đĩa rồi mới xóa lại.
        if (imageFile != null && imageFile.Length > 0 && ModelState.IsValid)
        {
            savedFileName = await SaveImageFileAsync(imageFile);
        }

        if (!ModelState.IsValid)
        {
            DeleteImageFileIfExists(savedFileName);

            await LoadCategoriesAsync(product.CategoryId);

            return View(
                "~/Views/Admin/Product/Create.cshtml",
                product);
        }

        // Ưu tiên file upload
        if (savedFileName != null)
        {
            product.Image = savedFileName;
        }
        else if (!string.IsNullOrWhiteSpace(cleanedImageUrl))
        {
            product.Image = cleanedImageUrl;
        }
        else
        {
            product.Image = "";
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đã thêm sản phẩm thành công.";

        return RedirectToAction(nameof(Index));
    }

    //Sửa
    [HttpGet]
    public async Task<IActionResult> Edit(int? productid)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        if (productid == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(item => item.ProductId == productid.Value);

        if (product == null)
        {
            return NotFound();
        }

        await LoadCategoriesAsync(product.CategoryId);

        return View("~/Views/Admin/Product/Edit.cshtml", product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? productid,
        [Bind("ProductId,CategoryId,ProductName,Price,Description,Image,Status")] Product product,
        IFormFile? imageFile,
        string? imageUrl)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        if (productid == null || productid.Value != product.ProductId)
        {
            return NotFound();
        }

        product.ProductName = product.ProductName?.Trim() ?? "";
        product.Description = product.Description?.Trim() ?? "";

        // FIX: dùng chung hàm validate với Create thay vì lặp lại logic.
        await ValidateProductAsync(product, product.ProductId);

        ModelState.Remove(nameof(product.Image));
        ModelState.Remove(nameof(product.Category));

        if (imageFile != null &&
            !string.IsNullOrWhiteSpace(imageUrl))
        {
            ModelState.AddModelError(
                "Image",
                "Chỉ được chọn ảnh hoặc nhập link ảnh, không chọn cả hai.");
        }

        string? cleanedImageUrl = null;

        if (!string.IsNullOrWhiteSpace(imageUrl) &&
            !TryValidateImageUrl(imageUrl, out cleanedImageUrl))
        {
            ModelState.AddModelError(
                "Image",
                "Link ảnh không hợp lệ (phải bắt đầu bằng http:// hoặc https://).");
        }

        // FIX: fetch entity gốc từ DB (thay vì chỉ đọc Image) để có thể
        // gán lại field-by-field và tận dụng change tracking + xử lý
        // DbUpdateConcurrencyException, thay vì Update() nguyên object build
        // từ [Bind] (không rõ những field khác có bị ghi đè ngoài ý muốn không).
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(item => item.ProductId == product.ProductId);

        if (existingProduct == null)
        {
            return NotFound();
        }

        var oldImage = existingProduct.Image;

        string? savedFileName = null;

        if (imageFile != null && imageFile.Length > 0 && ModelState.IsValid)
        {
            savedFileName = await SaveImageFileAsync(imageFile);
        }

        if (!ModelState.IsValid)
        {
            DeleteImageFileIfExists(savedFileName);

            await LoadCategoriesAsync(product.CategoryId);
            return View("~/Views/Admin/Product/Edit.cshtml", product);
        }

        string newImage;

        if (savedFileName != null)
        {
            DeleteImageFileIfExists(oldImage);
            newImage = savedFileName;
        }
        else if (!string.IsNullOrWhiteSpace(cleanedImageUrl))
        {
            DeleteImageFileIfExists(oldImage);
            newImage = cleanedImageUrl;
        }
        else
        {
            newImage = oldImage ?? "";
        }

        existingProduct.CategoryId = product.CategoryId;
        existingProduct.ProductName = product.ProductName;
        existingProduct.Price = product.Price;
        existingProduct.Description = product.Description;
        existingProduct.Status = product.Status;
        existingProduct.Image = newImage;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Products.AnyAsync(item => item.ProductId == product.ProductId))
            {
                return NotFound();
            }
            throw;
        }

        TempData["Success"] = "Đã cập nhật sản phẩm.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? productid)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        if (productid == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ProductId == productid.Value);

        if (product == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Product/Delete.cshtml", product);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int productid)
    {
        if (!IsAdmin())
        {
            return RedirectUnauthorized();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(item => item.ProductId == productid);

        if (product == null)
        {
            return NotFound();
        }

        var hasOrderDetail = await _context.OrderDetails
            .AnyAsync(item => item.ProductId == productid);

        var hasInvoiceDetail = await _context.InvoiceDetails
            .AnyAsync(item => item.ProductId == productid);

        var hasCartItem = await _context.CartItems
            .AnyAsync(item => item.ProductId == productid);

        // FIX: bản gốc không check Inventories và PromotionDetails — sản phẩm
        // đang có bản ghi tồn kho hoặc đang nằm trong khuyến mãi vẫn có thể bị
        // xóa cứng, dẫn tới lỗi ràng buộc khóa ngoại hoặc mất dữ liệu liên quan
        // một cách âm thầm. Thêm 2 điều kiện này vào cùng nhóm "có phát sinh dữ liệu".
        var hasInventory = await _context.Inventories
            .AnyAsync(item => item.ProductId == productid);

        var hasPromotion = await _context.PromotionDetails
            .AnyAsync(item => item.ProductId == productid);

        if (hasOrderDetail || hasInvoiceDetail || hasCartItem || hasInventory || hasPromotion)
        {
            product.Status = false;

            await _context.SaveChangesAsync();

            TempData["Error"] =
                "Sản phẩm đã phát sinh dữ liệu (đơn hàng/hóa đơn/giỏ hàng/tồn kho/khuyến mãi) " +
                "nên không thể xóa. Hệ thống đã chuyển sang ngừng bán.";

            return RedirectToAction(nameof(Index));
        }

        var imageToDelete = product.Image;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        DeleteImageFileIfExists(imageToDelete);

        TempData["Success"] = "Đã xóa sản phẩm.";

        return RedirectToAction(nameof(Index));
    }

    //Cập nhật trạng thái
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int productId, bool status)
    {
        if (!IsAdmin())
        {
            return Json(new { success = false, message = "Không có quyền." });
        }

        var product = await _context.Products.FindAsync(productId);

        if (product == null)
        {
            return NotFound();
        }

        product.Status = status;
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }
}