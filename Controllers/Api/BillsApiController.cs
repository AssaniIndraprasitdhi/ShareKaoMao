using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareKaoMao.Data;
using ShareKaoMao.Models;
using ShareKaoMao.Services;
using ShareKaoMao.ViewModels;

namespace ShareKaoMao.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BillsApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BillCalculationService _calculationService;

        public BillsApiController(AppDbContext context, BillCalculationService calculationService)
        {
            _context = context;
            _calculationService = calculationService;
        }

        /// <summary>ดึงรายการบิลทั้งหมด</summary>
        [HttpGet]
        public async Task<ActionResult<List<BillListItemViewModel>>> GetBills()
        {
            var bills = await _context.Bills
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BillListItemViewModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    CreatedAt = b.CreatedAt,
                    PeopleCount = b.People.Count,
                    ItemCount = b.Items.Count
                })
                .ToListAsync();

            return Ok(bills);
        }

        /// <summary>ดึงรายละเอียดบิล พร้อมผลคำนวณ</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult> GetBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.People)
                    .ThenInclude(p => p.ItemShares)
                        .ThenInclude(s => s.Item)
                            .ThenInclude(i => i.ItemShares)
                .Include(b => b.Items)
                    .ThenInclude(i => i.ItemShares)
                        .ThenInclude(s => s.Person)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
                return NotFound(new { message = "ไม่พบบิล" });

            var summaries = _calculationService.Calculate(bill);

            return Ok(new
            {
                bill.Id,
                bill.Title,
                bill.CreatedAt,
                bill.VatPercent,
                bill.ServicePercent,
                bill.TipAmount,
                bill.RoundingMode,
                People = bill.People.OrderBy(p => p.Id).Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Instagram
                }),
                Items = bill.Items.OrderBy(i => i.Id).Select(i => new
                {
                    i.Id,
                    i.Name,
                    i.Price,
                    i.Quantity,
                    Total = i.Price * i.Quantity,
                    SharedWith = i.ItemShares.Select(s => new { s.Person.Id, s.Person.Name })
                }),
                Summaries = summaries,
                BillTotal = summaries.Sum(s => s.RoundedTotal)
            });
        }

        /// <summary>สร้างบิลใหม่</summary>
        [HttpPost]
        public async Task<ActionResult> CreateBill([FromBody] BillCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var bill = new Bill
            {
                Title = model.Title.Trim(),
                CreatedAt = DateTime.UtcNow,
                VatPercent = 0,
                ServicePercent = 0,
                TipAmount = 0,
                RoundingMode = RoundingMode.None
            };

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBill), new { id = bill.Id }, new { bill.Id, bill.Title });
        }

        /// <summary>ลบบิล</summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBill(int id)
        {
            var bill = await _context.Bills.FindAsync(id);
            if (bill == null)
                return NotFound(new { message = "ไม่พบบิล" });

            _context.Bills.Remove(bill);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>เพิ่มคนในบิล</summary>
        [HttpPost("{billId}/people")]
        public async Task<ActionResult> AddPerson(int billId, [FromBody] AddPersonViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var bill = await _context.Bills
                .Include(b => b.People)
                .FirstOrDefaultAsync(b => b.Id == billId);

            if (bill == null)
                return NotFound(new { message = "ไม่พบบิล" });

            var name = model.Name.Trim();

            if (bill.People.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return Conflict(new { message = $"ชื่อ \"{name}\" มีอยู่ในบิลนี้แล้ว" });

            var instagram = model.Instagram?.Trim();
            var person = new Person
            {
                Name = name,
                Instagram = string.IsNullOrWhiteSpace(instagram) ? null : instagram,
                BillId = billId
            };
            _context.People.Add(person);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBill), new { id = billId }, new { person.Id, person.Name, person.Instagram });
        }

        /// <summary>ลบคนออกจากบิล</summary>
        [HttpDelete("{billId}/people/{personId}")]
        public async Task<ActionResult> DeletePerson(int billId, int personId)
        {
            var person = await _context.People
                .FirstOrDefaultAsync(p => p.Id == personId && p.BillId == billId);

            if (person == null)
                return NotFound(new { message = "ไม่พบคนในบิล" });

            _context.People.Remove(person);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>เพิ่มรายการในบิล</summary>
        [HttpPost("{billId}/items")]
        public async Task<ActionResult> AddItem(int billId, [FromBody] AddItemViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.SelectedPersonIds == null || model.SelectedPersonIds.Count == 0)
                return BadRequest(new { message = "กรุณาเลือกคนแชร์อย่างน้อย 1 คน" });

            var bill = await _context.Bills.FindAsync(billId);
            if (bill == null)
                return NotFound(new { message = "ไม่พบบิล" });

            var item = new Item
            {
                Name = model.Name.Trim(),
                Price = model.Price,
                Quantity = model.Quantity,
                BillId = billId
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            foreach (var personId in model.SelectedPersonIds)
            {
                _context.ItemShares.Add(new ItemShare
                {
                    ItemId = item.Id,
                    PersonId = personId
                });
            }
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBill), new { id = billId }, new { item.Id, item.Name, item.Price, item.Quantity });
        }

        /// <summary>ลบรายการออกจากบิล</summary>
        [HttpDelete("{billId}/items/{itemId}")]
        public async Task<ActionResult> DeleteItem(int billId, int itemId)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == itemId && i.BillId == billId);

            if (item == null)
                return NotFound(new { message = "ไม่พบรายการ" });

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>อัพเดท VAT / Service / Tip / Rounding</summary>
        [HttpPut("{billId}/extras")]
        public async Task<ActionResult> UpdateExtras(int billId, [FromBody] UpdateExtrasViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var bill = await _context.Bills.FindAsync(billId);
            if (bill == null)
                return NotFound(new { message = "ไม่พบบิล" });

            bill.VatPercent = model.VatPercent;
            bill.ServicePercent = model.ServicePercent;
            bill.TipAmount = model.TipAmount;
            bill.RoundingMode = model.RoundingMode;

            await _context.SaveChangesAsync();

            return Ok(new { message = "อัพเดทเรียบร้อย" });
        }
    }
}
