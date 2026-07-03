using System;
using System.Collections.Generic;

namespace FastBite_PRO231.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int UserId { get; set; }

    public string Position { get; set; } = null!;

    public DateTime HireDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;
}
