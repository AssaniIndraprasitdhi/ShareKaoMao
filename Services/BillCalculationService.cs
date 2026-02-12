using System;
using System.Collections.Generic;
using System.Linq;
using ShareKaoMao.Models;
using ShareKaoMao.ViewModels;

namespace ShareKaoMao.Services
{
    /// <summary>
    /// Service สำหรับคำนวณการหารค่าใช้จ่ายในบิล
    /// </summary>
    public class BillCalculationService
    {
        /// <summary>
        /// คำนวณยอดที่แต่ละคนต้องจ่ายในบิล
        ///
        /// ขั้นตอนการคำนวณ:
        /// 1. คำนวณ Subtotal ต่อคน = ผลรวม share ของแต่ละ item
        ///    - ItemTotal = Price × Quantity
        ///    - ถ้า item ถูกแชร์ N คน: แต่ละคนได้ ItemTotal / N
        ///
        /// 2. คำนวณ Extras ต่อคน (สัดส่วนตาม subtotal):
        ///    - VAT share = subtotal × VatPercent / 100
        ///    - Service share = subtotal × ServicePercent / 100
        ///    - Tip share = TipAmount × (subtotal / subtotal_รวมทั้งหมด)
        ///    * กรณี subtotal รวม = 0 → แจก extras เท่ากันทุกคน
        ///
        /// 3. GrandTotal ต่อคน = subtotal + VAT + Service + Tip
        ///
        /// 4. Rounding: ปัดขึ้นตาม mode ที่เลือก
        ///    จากนั้นปรับคนสุดท้ายเพื่อให้ยอดรวมตรงกับ actual total
        /// </summary>
        public List<PersonSummaryViewModel> Calculate(Bill bill)
        {
            var summaries = new List<PersonSummaryViewModel>();

            if (bill.People == null || bill.People.Count == 0)
                return summaries;

            int peopleCount = bill.People.Count;

            // ========================================
            // Step 1: คำนวณ Subtotal ต่อคน
            // Subtotal = ผลรวมของ (ItemTotal / จำนวนคนแชร์) สำหรับทุก item ที่คนนั้นแชร์
            // ========================================
            var subtotals = new Dictionary<int, decimal>();

            foreach (var person in bill.People)
            {
                decimal subtotal = 0m;

                if (person.ItemShares != null)
                {
                    foreach (var share in person.ItemShares)
                    {
                        if (share.Item == null) continue;

                        var item = share.Item;
                        decimal itemTotal = item.Price * item.Quantity;
                        int sharerCount = item.ItemShares?.Count ?? 1;

                        if (sharerCount > 0)
                        {
                            // แต่ละคนจ่าย = ราคารวมของ item / จำนวนคนที่แชร์
                            subtotal += itemTotal / sharerCount;
                        }
                    }
                }

                subtotals[person.Id] = subtotal;
            }

            // ========================================
            // Step 2: คำนวณ Total Subtotal ของทุกคนรวมกัน
            // ========================================
            decimal totalSubtotal = subtotals.Values.Sum();

            // ========================================
            // Step 3: คำนวณ Extras + GrandTotal ต่อคน
            // ========================================
            foreach (var person in bill.People)
            {
                var summary = new PersonSummaryViewModel
                {
                    PersonId = person.Id,
                    Name = person.Name,
                    Subtotal = subtotals[person.Id]
                };

                if (totalSubtotal > 0)
                {
                    // คำนวณสัดส่วนตาม subtotal
                    decimal proportion = summary.Subtotal / totalSubtotal;

                    // VAT = subtotal ของคนนั้น × VatPercent / 100
                    summary.VatShare = summary.Subtotal * bill.VatPercent / 100m;

                    // Service = subtotal ของคนนั้น × ServicePercent / 100
                    summary.ServiceShare = summary.Subtotal * bill.ServicePercent / 100m;

                    // Tip = TipAmount × (subtotal ของคนนั้น / subtotal รวมทั้งหมด)
                    summary.TipShare = bill.TipAmount * proportion;
                }
                else
                {
                    // กรณีไม่มี item เลย → แจก extras เท่ากันทุกคน
                    // VAT และ Service จะเป็น 0 เพราะคิดจาก subtotal (= 0)
                    summary.VatShare = 0m;
                    summary.ServiceShare = 0m;

                    // Tip หารเท่าๆ กัน
                    summary.TipShare = bill.TipAmount / peopleCount;
                }

                // GrandTotal = subtotal + VAT + Service + Tip
                summary.GrandTotal = summary.Subtotal
                    + summary.VatShare
                    + summary.ServiceShare
                    + summary.TipShare;

                // ตั้งค่า RoundedTotal เริ่มต้น = GrandTotal (จะปรับทีหลังถ้ามี rounding)
                summary.RoundedTotal = summary.GrandTotal;

                summaries.Add(summary);
            }

            // ========================================
            // Step 4: Apply Rounding
            //
            // กติกา:
            // - ปัดขึ้นแต่ละคน (ยกเว้นคนสุดท้าย) ตาม RoundingMode
            // - คนสุดท้าย = actual total - ผลรวมคนอื่น
            // - เหตุผล: การปัดขึ้นทุกคนจะทำให้ยอดรวมเกินจริง
            //   จึงต้องปรับคนสุดท้ายเพื่อให้ยอดรวมตรงกับ actual total
            //   ความแตกต่างจะน้อยมาก (ไม่เกินค่า rounding unit)
            // ========================================
            if (bill.RoundingMode != RoundingMode.None && summaries.Count > 0)
            {
                // Actual total ก่อนปัด (ค่าที่ต้องจ่ายจริงทั้งหมด)
                decimal actualTotal = summaries.Sum(s => s.GrandTotal);

                // ปัดขึ้นทุกคน ยกเว้นคนสุดท้าย
                for (int i = 0; i < summaries.Count - 1; i++)
                {
                    summaries[i].RoundedTotal = ApplyRounding(summaries[i].GrandTotal, bill.RoundingMode);
                }

                // คนสุดท้าย = actual total - ผลรวมคนอื่นที่ปัดแล้ว
                // ทำให้ยอดรวมทั้งหมดตรงกับ actual total พอดี
                decimal sumOfOthers = 0m;
                for (int i = 0; i < summaries.Count - 1; i++)
                {
                    sumOfOthers += summaries[i].RoundedTotal;
                }
                summaries[summaries.Count - 1].RoundedTotal = actualTotal - sumOfOthers;
            }

            return summaries;
        }

        /// <summary>
        /// ปัดขึ้นตาม RoundingMode
        /// - Round1:  ปัดขึ้นใกล้ 1 บาท  (เช่น 123.45 → 124)
        /// - Round5:  ปัดขึ้นใกล้ 5 บาท  (เช่น 123 → 125)
        /// - Round10: ปัดขึ้นใกล้ 10 บาท (เช่น 123 → 130)
        /// </summary>
        private decimal ApplyRounding(decimal value, RoundingMode mode)
        {
            switch (mode)
            {
                case RoundingMode.Round1:
                    return Math.Ceiling(value);

                case RoundingMode.Round5:
                    return Math.Ceiling(value / 5m) * 5m;

                case RoundingMode.Round10:
                    return Math.Ceiling(value / 10m) * 10m;

                default:
                    return value;
            }
        }
    }
}
