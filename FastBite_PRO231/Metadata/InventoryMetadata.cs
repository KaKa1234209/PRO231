using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.Metadata
{
    public class InventoryMetadata
    {
        [Required(ErrorMessage = "Số lượng không được để trống")]
        [Range(0, 999999, ErrorMessage = "Số lượng phải từ 0 trở lên")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Đơn vị tính không được để trống")]
        [StringLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự")]
        public string Unit { get; set; }
    }

    [ModelMetadataType(typeof(InventoryMetadata))]
    public partial class Inventory
    {
    }
}
