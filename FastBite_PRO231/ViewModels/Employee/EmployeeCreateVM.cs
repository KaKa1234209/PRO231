using System;

namespace FastBite_PRO231.Models
{
    public class EmployeeCreateVM
    {
        public int EmployeeId { get; set; }
        public int UserId { get; set; }

        // USER
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        // EMPLOYEE
        public string FullName { get; set; }
        public string Position { get; set; }
        public DateTime HireDate { get; set; }
        public string Status { get; set; }
    }
}