using System.ComponentModel.DataAnnotations;

namespace ShareKaoMao.ViewModels
{
    public class AddPersonViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อ")]
        [StringLength(100, ErrorMessage = "ชื่อยาวเกินไป")]
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "ไอจียาวเกินไป")]
        public string Instagram { get; set; }
    }
}
