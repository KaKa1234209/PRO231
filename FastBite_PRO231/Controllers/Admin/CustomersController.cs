
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FastBite_PRO231.Models;

public class CustomersController : Controller
{
    private readonly FastBiteDbContext _context;

    public CustomersController(FastBiteDbContext context)
    {
        _context = context;
    }

    // Trang chủ: Danh sách + tìm kiếm
    public async Task<IActionResult> Index(string searchString)
    {
        var customer = _context.Employees.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            customer = customer.Where(c =>
                c.FullName.Contains(searchString));
        }

        return View(await customer.ToListAsync());
    }

    // GET: CUSTOMERS/Details/5
    public async Task<IActionResult> Details(int? customerid)
    {
        if (customerid == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.CustomerId == customerid);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // GET: CUSTOMERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CUSTOMERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustomerId,UserId,FullName,Address,Point,Cart,Orders,User")] Customer customer)
    {
        if (ModelState.IsValid)
        {
            _context.Add(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    // GET: CUSTOMERS/Edit/5
    public async Task<IActionResult> Edit(int? customerid)
    {
        if (customerid == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FindAsync(customerid);
        if (customer == null)
        {
            return NotFound();
        }
        return View(customer);
    }

    // POST: CUSTOMERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? customerid, [Bind("CustomerId,UserId,FullName,Address,Point,Cart,Orders,User")] Customer customer)
    {
        if (customerid != customer.CustomerId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(customer.CustomerId))
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
        return View(customer);
    }

    // GET: CUSTOMERS/Delete/5
    public async Task<IActionResult> Delete(int? customerid)
    {
        if (customerid == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.CustomerId == customerid);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // POST: CUSTOMERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? customerid)
    {
        var customer = await _context.Customers.FindAsync(customerid);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CustomerExists(int? customerid)
    {
        return _context.Customers.Any(e => e.CustomerId == customerid);
    }
}
