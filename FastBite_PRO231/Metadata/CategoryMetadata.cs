using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.Models;

[ModelMetadataType(typeof(CategoryMetadata))]
public partial class Category
{
}

public class CategoryMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự.")]
    [Display(Name = "Tên danh mục")]
    public string CategoryName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}