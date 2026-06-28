using FastBite_PRO231.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.Metadata
{
    public class InventoryMetadata
    {
        [Required(ErrorMessage = "Số lượng không được để trống")]
        [Range(0, 999999, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Đơn vị tính không được để trống")]
        [StringLength(20)]
        public string Unit { get; set; }
    }

    [ModelMetadataType(typeof(InventoryMetadata))]
    public partial class Inventory
    {
    }
}
