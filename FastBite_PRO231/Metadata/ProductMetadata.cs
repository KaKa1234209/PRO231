using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Models
{
    public class ProductMetadata
    {
        [Required(ErrorMessage = "Tên món ăn không được để trống")]
        [StringLength(100,
            ErrorMessage = "Tên món ăn tối đa 100 ký tự")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Giá bán không được để trống")]
        [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hình ảnh sản phẩm")]
        public string Image { get; set; }
    }

    [ModelMetadataType(typeof(ProductMetadata))]
    public partial class Product
    {
    }
}