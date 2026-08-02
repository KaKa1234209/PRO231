using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.Models;

// FIX: cùng lý do như Category — Product.cs bị scaffold tự động, không sửa
// trực tiếp. Dùng buddy class để gắn validation an toàn.
//
// LƯU Ý: các check cần truy vấn DB (category có tồn tại không, tên sản phẩm
// có trùng không) KHÔNG thể làm bằng Data Annotation thuần — annotation chạy
// trước khi model được bind xong và không có quyền truy cập DbContext.
// Những check đó vẫn cần giữ trong ValidateProductAsync() ở controller,
// annotation ở đây chỉ thay cho phần "hình thức" (bắt buộc nhập, khoảng giá trị).
[ModelMetadataType(typeof(ProductMetadata))]
public partial class Product
{
}

public class ProductMetadata
{
    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string ProductName { get; set; } = null!;

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
    [Display(Name = "Giá")]
    public decimal Price { get; set; }

    [StringLength(2000, ErrorMessage = "Mô tả tối đa 2000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    // Image không gắn [Required]/[Url] ở đây vì logic thật (file upload vs link,
    // giữ ảnh cũ nếu không đổi...) đã được xử lý riêng trong controller
    // (TryValidateImageUrl, SaveImageFileAsync) và ModelState.Remove(nameof(product.Image))
    // được gọi tay trong controller — annotation ở đây sẽ không có tác dụng
    // (và không nên có, vì Image được set lại sau khi validate xong).
    public string? Image { get; set; }
}