using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Models
{
    public class EmployeeMetadata
    {
        [Required(ErrorMessage = "Chức vụ không được để trống")]
        [StringLength(50)]
        public string Position { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        public string Status { get; set; }
    }

    [ModelMetadataType(typeof(EmployeeMetadata))]
    public partial class Employee
    {
    }
}