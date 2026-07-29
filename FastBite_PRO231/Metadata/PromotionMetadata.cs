using FastBite_PRO231.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.Metadata
{
    public class PromotionMetadata
    {
        [Required(ErrorMessage = "Tên chương trình khuyến mãi không được để trống")]
        [StringLength(100, ErrorMessage = "Tên chương trình tối đa 100 ký tự")]
        public string PromotionName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại giảm giá")]
        public string DiscountType { get; set; }

        [Range(typeof(decimal), "0", "100", ErrorMessage = "Giá trị giảm giá phải từ 0 đến 100")]
        public decimal DiscountValue { get; set; }
    }
    [ModelMetadataType(typeof(PromotionMetadata))]
    public partial class Product
    {
    }
}
