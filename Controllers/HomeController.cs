using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShareKaoMao.Data;
using ShareKaoMao.ViewModels;

namespace ShareKaoMao.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var bills = _context.Bills
                .OrderByDescending(b => b.CreatedAt)
                .Take(20)
                .Select(b => new BillListItemViewModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    CreatedAt = b.CreatedAt,
                    PeopleCount = b.People.Count,
                    ItemCount = b.Items.Count
                })
                .ToList();

            return View(bills);
        }
    }
}
