using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShareKaoMao.ViewModels
{
    public class AddItemViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อรายการ")]
        [StringLength(200)]
        public string Name { get; set; }

        [Required(ErrorMessage = "กรุณากรอกราคา")]
        [Range(0.01, double.MaxValue, ErrorMessage = "ราคาต้องมากกว่า 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "กรุณากรอกจำนวน")]
        [Range(1, int.MaxValue, ErrorMessage = "จำนวนต้องอย่างน้อย 1")]
        public int Quantity { get; set; } = 1;

        public List<int> SelectedPersonIds { get; set; } = new List<int>();
    }
}
