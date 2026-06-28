using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Models
{
    public class ProductMetadata
    {
        [Required(ErrorMessage = "Tên món ăn không được để trống")]
        [StringLength(100)]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Giá bán không được để trống")]
        [Range(0, int.MaxValue,
            ErrorMessage = "Giá bán phải từ 0 trở lên")]
        public decimal Price { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hình ảnh")]
        public string Image { get; set; }
    }

    [ModelMetadataType(typeof(ProductMetadata))]
    public partial class Product
    {
    }
}