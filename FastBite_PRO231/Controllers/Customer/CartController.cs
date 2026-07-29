using FastBite_PRO231.Models;
using FastBite_PRO231.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.Controllers;

public class CartController : Controller
{
    private readonly FastBiteDbContext _context;

    public CartController(FastBiteDbContext context)
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

    private async Task<Cart> GetOrCreateCartAsync(int customerId)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(item =>
                item.CustomerId == customerId);

        if (cart != null)
        {
            return cart;
        }

        cart = new Cart
        {
            CustomerId = customerId,
            CreatedAt = DateTime.Now,
            TotalPrice = 0
        };

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        return cart;
    }

    private async Task RecalculateCartAsync(int cartId)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(item =>
                item.CartId == cartId);

        if (cart == null)
        {
            return;
        }

        cart.TotalPrice = await _context.CartItems
            .Where(item => item.CartId == cartId)
            .SumAsync(item => (decimal?)item.SubTotal)
            ?? 0m;

        await _context.SaveChangesAsync();
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

    // GET: /Cart
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsCustomer())
        {
            TempData["Error"] =
                "Vui lòng đăng nhập tài khoản khách hàng.";

            return RedirectToAction(
                "Login",
                "Login");
        }

        var customer = await GetCurrentCustomerAsync();

        if (customer == null)
        {
            TempData["Error"] =
                "Không tìm thấy hồ sơ khách hàng.";

            return RedirectToAction(
                "Login",
                "Login");
        }

        var cart = await GetOrCreateCartAsync(
            customer.CustomerId);

        var cartItems = await _context.CartItems
            .AsNoTracking()
            .Include(item => item.Product)
            .Where(item => item.CartId == cart.CartId)
            .OrderByDescending(item => item.CartItemId)
            .ToListAsync();

        var productIds = cartItems
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();

        var stockDictionary = await _context.Inventories
            .AsNoTracking()
            .Where(inventory =>
                productIds.Contains(inventory.ProductId))
            .ToDictionaryAsync(
                inventory => inventory.ProductId,
                inventory => inventory.Quantity);

        var items = cartItems
            .Select(item => new CartItemViewModel
            {
                CartItemId = item.CartItemId,
                ProductId = item.ProductId,

                ProductName =
                    item.Product?.ProductName
                    ?? "Sản phẩm",

                ImageUrl = NormalizeImageUrl(
                    item.Product?.Image),

                Quantity = item.Quantity,

                StockQuantity =
                    stockDictionary.TryGetValue(
                        item.ProductId,
                        out var stock)
                        ? stock
                        : 0,

                Price = item.Price,
                SubTotal = item.SubTotal
            })
            .ToList();

        var model = new CartViewModel
        {
            CartId = cart.CartId,
            Items = items,

            TotalQuantity = items.Sum(
                item => item.Quantity),

            TotalPrice = items.Sum(
                item => item.SubTotal)
        };

        return View(model);
    }

    // POST: /Cart/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        int productId,
        int quantity = 1)
    {
        if (!IsCustomer())
        {
            TempData["Error"] =
                "Bạn cần đăng nhập tài khoản khách hàng để đặt món.";

            return RedirectToAction(
                "Login",
                "Login");
        }

        var customer = await GetCurrentCustomerAsync();

        if (customer == null)
        {
            TempData["Error"] =
                "Không tìm thấy hồ sơ khách hàng.";

            return RedirectToAction(
                "Login",
                "Login");
        }

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.ProductId == productId &&
                item.Status);

        if (product == null)
        {
            TempData["Error"] =
                "Sản phẩm không tồn tại hoặc đã ngừng bán.";

            return RedirectToAction(
                "Index",
                "Home");
        }

        var inventory = await _context.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.ProductId == productId);

        if (inventory == null ||
            inventory.Quantity <= 0)
        {
            TempData["Error"] =
                "Sản phẩm hiện đã hết hàng.";

            return RedirectToAction(
                "Index",
                "Home");
        }

        quantity = Math.Max(quantity, 1);

        var cart = await GetOrCreateCartAsync(
            customer.CustomerId);

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(item =>
                item.CartId == cart.CartId &&
                item.ProductId == productId);

        var newQuantity =
            (cartItem?.Quantity ?? 0) + quantity;

        if (newQuantity > inventory.Quantity)
        {
            TempData["Error"] =
                $"Chỉ còn {inventory.Quantity} phần trong kho.";

            return RedirectToAction(nameof(Index));
        }

        if (cartItem == null)
        {
            cartItem = new CartItem
            {
                CartId = cart.CartId,
                ProductId = product.ProductId,
                Quantity = quantity,
                Price = product.Price,
                SubTotal = product.Price * quantity
            };

            _context.CartItems.Add(cartItem);
        }
        else
        {
            cartItem.Quantity = newQuantity;
            cartItem.Price = product.Price;
            cartItem.SubTotal =
                cartItem.Price * cartItem.Quantity;
        }

        await _context.SaveChangesAsync();
        await RecalculateCartAsync(cart.CartId);

        TempData["Success"] =
            $"Đã thêm {product.ProductName} vào giỏ hàng.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/UpdateQuantity
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(
        int cartItemId,
        int quantity)
    {
        var userId =
            HttpContext.Session.GetInt32("UserId");

        if (!IsCustomer() || !userId.HasValue)
        {
            return RedirectToAction(
                "Login",
                "Login");
        }

        var cartItem = await _context.CartItems
            .Include(item => item.Cart)
                .ThenInclude(cart => cart.Customer)
            .FirstOrDefaultAsync(item =>
                item.CartItemId == cartItemId &&
                item.Cart.Customer.UserId == userId.Value);

        if (cartItem == null)
        {
            TempData["Error"] =
                "Không tìm thấy món trong giỏ hàng.";

            return RedirectToAction(nameof(Index));
        }

        if (quantity <= 0)
        {
            var cartId = cartItem.CartId;

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            await RecalculateCartAsync(cartId);

            TempData["Success"] =
                "Đã xóa món khỏi giỏ hàng.";

            return RedirectToAction(nameof(Index));
        }

        var stock = await _context.Inventories
            .AsNoTracking()
            .Where(item =>
                item.ProductId == cartItem.ProductId)
            .Select(item => (int?)item.Quantity)
            .FirstOrDefaultAsync()
            ?? 0;

        if (quantity > stock)
        {
            TempData["Error"] =
                $"Sản phẩm chỉ còn {stock} phần.";

            return RedirectToAction(nameof(Index));
        }

        cartItem.Quantity = quantity;
        cartItem.SubTotal =
            cartItem.Price * cartItem.Quantity;

        await _context.SaveChangesAsync();
        await RecalculateCartAsync(cartItem.CartId);

        TempData["Success"] =
            "Đã cập nhật số lượng.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        int cartItemId)
    {
        var userId =
            HttpContext.Session.GetInt32("UserId");

        if (!IsCustomer() || !userId.HasValue)
        {
            return RedirectToAction(
                "Login",
                "Login");
        }

        var cartItem = await _context.CartItems
            .Include(item => item.Cart)
                .ThenInclude(cart => cart.Customer)
            .FirstOrDefaultAsync(item =>
                item.CartItemId == cartItemId &&
                item.Cart.Customer.UserId == userId.Value);

        if (cartItem == null)
        {
            TempData["Error"] =
                "Không tìm thấy món trong giỏ hàng.";

            return RedirectToAction(nameof(Index));
        }

        var cartId = cartItem.CartId;

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        await RecalculateCartAsync(cartId);

        TempData["Success"] =
            "Đã xóa món khỏi giỏ hàng.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/Clear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var userId =
            HttpContext.Session.GetInt32("UserId");

        if (!IsCustomer() || !userId.HasValue)
        {
            return RedirectToAction(
                "Login",
                "Login");
        }

        var cart = await _context.Carts
            .Include(item => item.CartItems)
            .FirstOrDefaultAsync(item =>
                item.Customer.UserId == userId.Value);

        if (cart == null)
        {
            return RedirectToAction(nameof(Index));
        }

        _context.CartItems.RemoveRange(
            cart.CartItems);

        cart.TotalPrice = 0;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Đã xóa toàn bộ giỏ hàng.";

        return RedirectToAction(nameof(Index));
    }
}