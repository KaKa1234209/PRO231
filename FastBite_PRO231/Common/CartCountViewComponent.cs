using FastBite_PRO231.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastBite_PRO231.ViewComponents;

public class CartCountViewComponent : ViewComponent
{
    private readonly FastBiteDbContext _context;

    public CartCountViewComponent(
        FastBiteDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var role =
            HttpContext.Session.GetString("Role");

        var userId =
            HttpContext.Session.GetInt32("UserId");

        if (!string.Equals(
                role,
                "Customer",
                StringComparison.OrdinalIgnoreCase) ||
            !userId.HasValue)
        {
            return Content("0");
        }

        var quantity = await _context.CartItems
            .AsNoTracking()
            .Where(item =>
                item.Cart.Customer.UserId ==
                userId.Value)
            .SumAsync(item => (int?)item.Quantity)
            ?? 0;

        return Content(quantity.ToString());
    }
}