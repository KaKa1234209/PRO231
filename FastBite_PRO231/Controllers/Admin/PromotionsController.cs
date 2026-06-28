
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FastBite_PRO231.Models;

public class PromotionsController : Controller
{
    private readonly FastBiteDbContext _context;

    public PromotionsController(FastBiteDbContext context)
    {
        _context = context;
    }

    // GET: PROMOTIONS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Promotions.ToListAsync());
    }

    // GET: PROMOTIONS/Details/5
    public async Task<IActionResult> Details(int? promotionid)
    {
        if (promotionid == null)
        {
            return NotFound();
        }

        var promotion = await _context.Promotions
            .FirstOrDefaultAsync(m => m.PromotionId == promotionid);
        if (promotion == null)
        {
            return NotFound();
        }

        return View(promotion);
    }

    // GET: PROMOTIONS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PROMOTIONS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PromotionId,PromotionName,DiscountType,DiscountValue,Status,PromotionDetails")] Promotion promotion)
    {
        if (ModelState.IsValid)
        {
            _context.Add(promotion);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(promotion);
    }

    // GET: PROMOTIONS/Edit/5
    public async Task<IActionResult> Edit(int? promotionid)
    {
        if (promotionid == null)
        {
            return NotFound();
        }

        var promotion = await _context.Promotions.FindAsync(promotionid);
        if (promotion == null)
        {
            return NotFound();
        }
        return View(promotion);
    }

    // POST: PROMOTIONS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? promotionid, [Bind("PromotionId,PromotionName,DiscountType,DiscountValue,Status,PromotionDetails")] Promotion promotion)
    {
        if (promotionid != promotion.PromotionId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(promotion);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PromotionExists(promotion.PromotionId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(promotion);
    }

    // GET: PROMOTIONS/Delete/5
    public async Task<IActionResult> Delete(int? promotionid)
    {
        if (promotionid == null)
        {
            return NotFound();
        }

        var promotion = await _context.Promotions
            .FirstOrDefaultAsync(m => m.PromotionId == promotionid);
        if (promotion == null)
        {
            return NotFound();
        }

        return View(promotion);
    }

    // POST: PROMOTIONS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? promotionid)
    {
        var promotion = await _context.Promotions.FindAsync(promotionid);
        if (promotion != null)
        {
            _context.Promotions.Remove(promotion);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PromotionExists(int? promotionid)
    {
        return _context.Promotions.Any(e => e.PromotionId == promotionid);
    }
}
