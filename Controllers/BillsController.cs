using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareKaoMao.Data;
using ShareKaoMao.Models;
using ShareKaoMao.Services;
using ShareKaoMao.ViewModels;

namespace ShareKaoMao.Controllers
{
    public class BillsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly BillCalculationService _calculationService;

        public BillsController(AppDbContext context, BillCalculationService calculationService)
        {
            _context = context;
            _calculationService = calculationService;
        }

        // GET: Bills/Create
        public IActionResult Create()
        {
            return View(new BillCreateViewModel());
        }

        // POST: Bills/Create (PRG → redirect ไป Details)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BillCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

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

            return RedirectToAction("Details", new { id = bill.Id });
        }

        // GET: Bills/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            // โหลดบิลพร้อม relations ทั้งหมดที่ต้องใช้ในการคำนวณ
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
                return NotFound();

            // คำนวณยอดต่อคน
            var summaries = _calculationService.Calculate(bill);

            var viewModel = new BillDetailsViewModel
            {
                BillId = bill.Id,
                Title = bill.Title,
                CreatedAt = bill.CreatedAt,
                VatPercent = bill.VatPercent,
                ServicePercent = bill.ServicePercent,
                TipAmount = bill.TipAmount,
                RoundingMode = bill.RoundingMode,
                People = bill.People
                    .OrderBy(p => p.Id)
                    .Select(p => new PersonDisplayViewModel
                    {
                        Id = p.Id,
                        Name = p.Name
                    }).ToList(),
                Items = bill.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new ItemDisplayViewModel
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Price = i.Price,
                        Quantity = i.Quantity,
                        Total = i.Price * i.Quantity,
                        SharedWith = i.ItemShares
                            .Select(s => s.Person.Name)
                            .OrderBy(n => n)
                            .ToList()
                    }).ToList(),
                Summaries = summaries,
                BillTotal = summaries.Sum(s => s.RoundedTotal),
                ExtrasForm = new UpdateExtrasViewModel
                {
                    VatPercent = bill.VatPercent,
                    ServicePercent = bill.ServicePercent,
                    TipAmount = bill.TipAmount,
                    RoundingMode = bill.RoundingMode
                }
            };

            ViewBag.Error = TempData["Error"];
            ViewBag.Success = TempData["Success"];

            return View(viewModel);
        }

        // POST: Bills/AddPerson/{billId} (PRG)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPerson(int billId, AddPersonViewModel model)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "กรุณากรอกชื่อ";
                return RedirectToAction("Details", new { id = billId });
            }

            var bill = await _context.Bills
                .Include(b => b.People)
                .FirstOrDefaultAsync(b => b.Id == billId);

            if (bill == null)
                return NotFound();

            var name = model.Name.Trim();

            // ตรวจสอบชื่อซ้ำ (case-insensitive)
            if (bill.People.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = $"ชื่อ \"{name}\" มีอยู่ในบิลนี้แล้ว";
                return RedirectToAction("Details", new { id = billId });
            }

            var person = new Person
            {
                Name = name,
                BillId = billId
            };

            _context.People.Add(person);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = billId });
        }

        // POST: Bills/DeletePerson/{billId}/{personId} (PRG)
        // ลบ Person พร้อม ItemShares ที่เกี่ยวข้อง (cascade delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePerson(int billId, int personId)
        {
            var person = await _context.People
                .FirstOrDefaultAsync(p => p.Id == personId && p.BillId == billId);

            if (person == null)
                return NotFound();

            // Cascade delete จะลบ ItemShares ที่เกี่ยวข้องอัตโนมัติ
            _context.People.Remove(person);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = billId });
        }

        // POST: Bills/AddItem/{billId} (PRG)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int billId, AddItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "กรุณากรอกข้อมูลรายการให้ครบถ้วน";
                return RedirectToAction("Details", new { id = billId });
            }

            // ต้องเลือกคนแชร์อย่างน้อย 1 คน
            if (model.SelectedPersonIds == null || model.SelectedPersonIds.Count == 0)
            {
                TempData["Error"] = "กรุณาเลือกคนแชร์อย่างน้อย 1 คน";
                return RedirectToAction("Details", new { id = billId });
            }

            // ป้องกันค่าติดลบ
            if (model.Price <= 0)
            {
                TempData["Error"] = "ราคาต้องมากกว่า 0";
                return RedirectToAction("Details", new { id = billId });
            }

            if (model.Quantity < 1)
            {
                TempData["Error"] = "จำนวนต้องอย่างน้อย 1";
                return RedirectToAction("Details", new { id = billId });
            }

            var bill = await _context.Bills.FindAsync(billId);
            if (bill == null)
                return NotFound();

            var item = new Item
            {
                Name = model.Name.Trim(),
                Price = model.Price,
                Quantity = model.Quantity,
                BillId = billId
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // สร้าง ItemShares สำหรับแต่ละคนที่เลือก
            foreach (var personId in model.SelectedPersonIds)
            {
                _context.ItemShares.Add(new ItemShare
                {
                    ItemId = item.Id,
                    PersonId = personId
                });
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = billId });
        }

        // POST: Bills/DeleteItem/{billId}/{itemId} (PRG)
        // ลบ Item พร้อม ItemShares ที่เกี่ยวข้อง (cascade delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int billId, int itemId)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == itemId && i.BillId == billId);

            if (item == null)
                return NotFound();

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = billId });
        }

        // POST: Bills/UpdateExtras/{billId} (PRG)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExtras(int billId, UpdateExtrasViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "กรุณากรอกข้อมูลให้ถูกต้อง";
                return RedirectToAction("Details", new { id = billId });
            }

            // ป้องกันค่าติดลบ
            if (model.VatPercent < 0 || model.ServicePercent < 0 || model.TipAmount < 0)
            {
                TempData["Error"] = "ค่า VAT, Service, Tip ต้องไม่ติดลบ";
                return RedirectToAction("Details", new { id = billId });
            }

            var bill = await _context.Bills.FindAsync(billId);
            if (bill == null)
                return NotFound();

            bill.VatPercent = model.VatPercent;
            bill.ServicePercent = model.ServicePercent;
            bill.TipAmount = model.TipAmount;
            bill.RoundingMode = model.RoundingMode;

            await _context.SaveChangesAsync();

            TempData["Success"] = "อัพเดทค่าใช้จ่ายเพิ่มเติมเรียบร้อย";
            return RedirectToAction("Details", new { id = billId });
        }
    }
}
