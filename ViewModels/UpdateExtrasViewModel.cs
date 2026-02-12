using System.ComponentModel.DataAnnotations;
using ShareKaoMao.Models;

namespace ShareKaoMao.ViewModels
{
    public class UpdateExtrasViewModel
    {
        [Range(0, 100, ErrorMessage = "VAT ต้องอยู่ระหว่าง 0-100%")]
        public decimal VatPercent { get; set; }

        [Range(0, 100, ErrorMessage = "Service Charge ต้องอยู่ระหว่าง 0-100%")]
        public decimal ServicePercent { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "ทิปต้องไม่ติดลบ")]
        public decimal TipAmount { get; set; }

        public RoundingMode RoundingMode { get; set; }
    }
}
