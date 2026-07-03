using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FastBite_PRO231.Models
{
    public class CategoryMetadata
    {
        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự")]
        public string CategoryName { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
        public string? Description { get; set; }
    }

    [ModelMetadataType(typeof(CategoryMetadata))]
    public partial class Category
    {
    }
}