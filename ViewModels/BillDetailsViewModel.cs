using System;
using System.Collections.Generic;
using ShareKaoMao.Models;

namespace ShareKaoMao.ViewModels
{
    public class BillDetailsViewModel
    {
        public int BillId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }

        // ค่าปัจจุบันของ Extras
        public decimal VatPercent { get; set; }
        public decimal ServicePercent { get; set; }
        public decimal TipAmount { get; set; }
        public RoundingMode RoundingMode { get; set; }

        // คนในบิล
        public List<PersonDisplayViewModel> People { get; set; } = new List<PersonDisplayViewModel>();

        // รายการในบิล
        public List<ItemDisplayViewModel> Items { get; set; } = new List<ItemDisplayViewModel>();

        // ผลคำนวณต่อคน
        public List<PersonSummaryViewModel> Summaries { get; set; } = new List<PersonSummaryViewModel>();

        // ยอดรวมทั้งบิล
        public decimal BillTotal { get; set; }

        // Sub-forms
        public AddPersonViewModel AddPersonForm { get; set; } = new AddPersonViewModel();
        public AddItemViewModel AddItemForm { get; set; } = new AddItemViewModel();
        public UpdateExtrasViewModel ExtrasForm { get; set; } = new UpdateExtrasViewModel();
    }

    public class PersonDisplayViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Instagram { get; set; }
    }

    public class ItemDisplayViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
        public List<string> SharedWith { get; set; } = new List<string>();
    }

    public class PersonSummaryViewModel
    {
        public int PersonId { get; set; }
        public string Name { get; set; }
        public decimal Subtotal { get; set; }
        public decimal VatShare { get; set; }
        public decimal ServiceShare { get; set; }
        public decimal TipShare { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal RoundedTotal { get; set; }
    }

    public class BillListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PeopleCount { get; set; }
        public int ItemCount { get; set; }
    }
}
