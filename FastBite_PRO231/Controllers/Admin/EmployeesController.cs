
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FastBite_PRO231.Models;

public class EmployeesController : Controller
{
    private readonly FastBiteDbContext _context;

    public EmployeesController(FastBiteDbContext context)
    {
        _context = context;
    }

    // Trang chủ: Danh sách + tìm kiếm
    public async Task<IActionResult> Index(string searchString)
    {
        var employees = _context.Employees.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            employees = employees.Where(c =>
                c.FullName.Contains(searchString));
        }

        return View(await employees.ToListAsync());
    }

    // Chi tiết
    public async Task<IActionResult> Details(int? employeeid)
    {
        if (employeeid == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(m => m.EmployeeId == employeeid);
        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }


    // Thêm
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("EmployeeId,UserId,FullName,Position,HireDate,Status")] Employee employee)
    {
        if (ModelState.IsValid)
        {
            _context.Add(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(employee);
    }

    // Sửa
    public async Task<IActionResult> Edit(int? employeeid)
    {
        if (employeeid == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees.FindAsync(employeeid);

        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? employeeid, [Bind("EmployeeId,UserId,FullName,Position,HireDate,Status")] Employee employee)
    {
        if (employeeid != employee.EmployeeId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(employee);

                var user = await _context.Users.FindAsync(employee.UserId);

                if (user != null)
                {
                    if (employee.Status == "Đã nghỉ việc")
                    {
                        user.Status = "Ngừng hoạt động";
                    }
                    else
                    {
                        user.Status = "Hoạt động";
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(employee.EmployeeId))
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
        return View(employee);
    }

    private bool EmployeeExists(int? employeeid)
    {
        return _context.Employees.Any(e => e.EmployeeId == employeeid);
    }
}
