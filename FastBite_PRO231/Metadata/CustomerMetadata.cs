using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Models
{
    public class CustomerMetadata
    {
        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(255)]
        public string Address { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Điểm tích lũy không hợp lệ")]
        public int Point { get; set; }
    }

    [ModelMetadataType(typeof(CustomerMetadata))]
    public partial class Customer
    {
    }
}