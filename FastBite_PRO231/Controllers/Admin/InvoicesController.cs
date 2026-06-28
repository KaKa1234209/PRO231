
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

    // GET: INVOICES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Invoices.ToListAsync());
    }

    // GET: INVOICES/Details/5
    public async Task<IActionResult> Details(int? invoiceid)
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

    // GET: INVOICES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: INVOICES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("InvoiceId,OrderId,EmployeeId,InvoiceDate,TotalAmount,PaymentMethod,Status,Employee,InvoiceDetails,Order")] Invoice invoice)
    {
        if (ModelState.IsValid)
        {
            _context.Add(invoice);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(invoice);
    }

    // GET: INVOICES/Edit/5
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

    // POST: INVOICES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? invoiceid, [Bind("InvoiceId,OrderId,EmployeeId,InvoiceDate,TotalAmount,PaymentMethod,Status,Employee,InvoiceDetails,Order")] Invoice invoice)
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

    // GET: INVOICES/Delete/5
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

    // POST: INVOICES/Delete/5
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
