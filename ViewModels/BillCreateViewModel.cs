using System.ComponentModel.DataAnnotations;

namespace ShareKaoMao.ViewModels
{
    public class BillCreateViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อบิล")]
        [StringLength(200, ErrorMessage = "ชื่อบิลยาวเกินไป")]
        public string Title { get; set; }
    }
}
