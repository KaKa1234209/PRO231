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
 
        // >>> MỚI: kiểm tra thêm Content-Type thực tế của file.
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
        product.ProductName = product.ProductName?.Trim() ?? "";
        product.Description = product.Description?.Trim() ?? "";
 
        // validation của bạn giữ nguyên ở đây
        ModelState.Remove(nameof(product.Image));
        ModelState.Remove(nameof(product.Category));
 
        // Không được nhập cả 2
        if (imageFile != null &&
            !string.IsNullOrWhiteSpace(imageUrl))
        {
            ModelState.AddModelError(
                "Image",
                "Chỉ được chọn ảnh hoặc nhập link ảnh, không chọn cả hai.");
        }
 
        // >>> SỬA: validate link ảnh chặt hơn (chỉ chấp nhận http/https)
        string? cleanedImageUrl = null;
 
        if (!string.IsNullOrWhiteSpace(imageUrl) &&
            !TryValidateImageUrl(imageUrl, out cleanedImageUrl))
        {
            ModelState.AddModelError(
                "Image",
                "Link ảnh không hợp lệ (phải bắt đầu bằng http:// hoặc https://).");
        }
 
        string? savedFileName = null;
 
        // >>> SỬA: chỉ lưu file khi các validate ở trên chưa có lỗi,
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
        string? imageUrl) // >>> MỚI: cho phép sửa ảnh bằng link, giống Create
    {
        if (productid == null || productid.Value != product.ProductId)
        {
            return NotFound();
        }
 
        product.ProductName = product.ProductName?.Trim() ?? "";
        product.Description = product.Description?.Trim() ?? "";
 
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
 
        if (product.Price <= 0)
        {
            ModelState.AddModelError(
                nameof(product.Price),
                "Giá sản phẩm phải lớn hơn 0.");
        }
 
        var nameExists = await _context.Products
            .AnyAsync(item =>
                item.ProductName == product.ProductName &&
                item.ProductId != product.ProductId);
 
        if (nameExists)
        {
            ModelState.AddModelError(
                nameof(product.ProductName),
                "Tên sản phẩm đã tồn tại.");
        }
 
        // Image lấy giá trị cũ/mới thông qua logic riêng bên dưới,
        // nên xóa lỗi validate tự động của MVC gán nhầm cho field này.
        ModelState.Remove(nameof(product.Image));
 
        // Category là navigation property, không được form gửi lên trực tiếp
        // (chỉ gửi CategoryId) -> xóa lỗi "required" tự động MVC gán nhầm.
        ModelState.Remove(nameof(product.Category));
 
        // >>> MỚI: không được chọn cả file lẫn link cùng lúc (giống Create)
        if (imageFile != null &&
            !string.IsNullOrWhiteSpace(imageUrl))
        {
            ModelState.AddModelError(
                "Image",
                "Chỉ được chọn ảnh hoặc nhập link ảnh, không chọn cả hai.");
        }
 
        // >>> MỚI: validate link ảnh (chỉ chấp nhận http/https)
        string? cleanedImageUrl = null;
 
        if (!string.IsNullOrWhiteSpace(imageUrl) &&
            !TryValidateImageUrl(imageUrl, out cleanedImageUrl))
        {
            ModelState.AddModelError(
                "Image",
                "Link ảnh không hợp lệ (phải bắt đầu bằng http:// hoặc https://).");
        }
 
        // Lấy ảnh cũ trong DB để biết có cần xóa/giữ khi có ảnh mới hay không.
        var oldImage = await _context.Products
            .AsNoTracking()
            .Where(item => item.ProductId == product.ProductId)
            .Select(item => item.Image)
            .FirstOrDefaultAsync();
 
        string? savedFileName = null;
 
        // >>> SỬA: chỉ lưu file khi các validate ở trên chưa có lỗi
        if (imageFile != null && imageFile.Length > 0 && ModelState.IsValid)
        {
            savedFileName = await SaveImageFileAsync(imageFile);
        }
 
        if (!ModelState.IsValid)
        {
            // Có ảnh mới lỡ lưu rồi nhưng dữ liệu khác lỗi -> xóa file vừa lưu.
            DeleteImageFileIfExists(savedFileName);
 
            await LoadCategoriesAsync(product.CategoryId);
            return View("~/Views/Admin/Product/Edit.cshtml", product);
        }
 
        if (savedFileName != null)
        {
            // Có ảnh mới từ file upload -> xóa ảnh cũ (nếu là file local) rồi dùng ảnh mới.
            DeleteImageFileIfExists(oldImage);
            product.Image = savedFileName;
        }
        else if (!string.IsNullOrWhiteSpace(cleanedImageUrl))
        {
            // >>> MỚI: có link ảnh mới -> xóa ảnh cũ (nếu là file local) rồi dùng link mới.
            DeleteImageFileIfExists(oldImage);
            product.Image = cleanedImageUrl;
        }
        else
        {
            // Không upload ảnh mới, không nhập link mới -> giữ nguyên ảnh cũ.
            product.Image = oldImage ?? "";
        }
 
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
 
        TempData["Success"] = "Đã cập nhật sản phẩm.";
 
        return RedirectToAction(nameof(Index));
    }
 
    [HttpGet]
    public async Task<IActionResult> Delete(int? productid)
    {
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
 
        if (hasOrderDetail || hasInvoiceDetail || hasCartItem)
        {
            product.Status = false;
 
            await _context.SaveChangesAsync();
 
            TempData["Error"] =
                "Sản phẩm đã phát sinh dữ liệu nên không thể xóa. " +
                "Hệ thống đã chuyển sang ngừng bán.";
 
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