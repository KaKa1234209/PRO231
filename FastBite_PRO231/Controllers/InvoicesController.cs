
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
    public async Task<IActionResult> Index(int? searchId, int? invoiceId, bool? status, DateTime? invoiceDate)
    {
        var invoices = _context.Invoices
            .Include(i => i.Order)
            .Include(i => i.Employee)
            .AsQueryable();
        //Tìm kiếm
        if (searchId.HasValue)
        {
            invoices = invoices.Where(i => i.InvoiceId == searchId);
        }

        // Lọc theo mã hóa đơn
        if (invoiceId.HasValue)
        {
            invoices = invoices.Where(i => i.InvoiceId == invoiceId.Value);
        }

        // Lọc theo trạng thái
        if (status.HasValue)
        {
            invoices = invoices.Where(i => i.Status == status.Value);
        }

        // Lọc theo ngày
        if (invoiceDate.HasValue)
        {
            invoices = invoices.Where(i =>
                i.InvoiceDate.Date == invoiceDate.Value.Date);
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

    //Hủy 
    [HttpPost]
    public async Task<IActionResult> Cancel(int invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);

        if (invoice == null)
        {
            return NotFound();
        }

        invoice.Status = false;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
