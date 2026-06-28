
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FastBite_PRO231.Models;

public class InvoicesController : Controller
{
    private readonly FastBiteDbContext _context;

    public InvoicesController(FastBiteDbContext context)
    {
        _context = context;
    }

    // Trang chủ: Danh sách + tìm kiếm
    public async Task<IActionResult> Index(int? searchId)
    {
        var invoices = _context.Invoices
            .Include(i => i.Order)
            .Include(i => i.Employee)
            .AsQueryable();

        if (searchId.HasValue)
        {
            invoices = invoices.Where(i => i.InvoiceId == searchId);
        }

        return View(await invoices.ToListAsync());
    }

    // Chi tiết
    public async Task<IActionResult> Details(int? invoiceid)
    {
        if (invoiceid == null)
        {
            return NotFound();
        }

        var invoice = await _context.Invoices
            .Include(i => i.InvoiceDetails)
            .ThenInclude(d => d.Product)
            .Include(i => i.Employee)
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceid);
        if (invoice == null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    // Thêm
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("OrderId,EmployeeId,InvoiceDate,TotalAmount,PaymentMethod,Status,Employee,InvoiceDetails,Order")] Invoice invoice)
    {
        if (ModelState.IsValid)
        {
            _context.Add(invoice);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(invoice);
    }

    // Sửa
    public async Task<IActionResult> Edit(int? invoiceid)
    {
        if (invoiceid == null)
        {
            return NotFound();
        }

        var invoice = await _context.Invoices.FindAsync(invoiceid);
        if (invoice == null)
        {
            return NotFound();
        }
        return View(invoice);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? invoiceid, [Bind("OrderId,EmployeeId,InvoiceDate,TotalAmount,PaymentMethod,Status,Employee,InvoiceDetails,Order")] Invoice invoice)
    {
        if (invoiceid != invoice.InvoiceId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceExists(invoice.InvoiceId))
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
        return View(invoice);
    }

    // Xóa
    public async Task<IActionResult> Delete(int? invoiceid)
    {
        if (invoiceid == null)
        {
            return NotFound();
        }

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(m => m.InvoiceId == invoiceid);
        if (invoice == null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? invoiceid)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceid);
        if (invoice != null)
        {
            _context.Invoices.Remove(invoice);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool InvoiceExists(int? invoiceid)
    {
        return _context.Invoices.Any(e => e.InvoiceId == invoiceid);
    }
}
