using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShareKaoMao.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public int BillId { get; set; }
        public Bill Bill { get; set; }

        public List<ItemShare> ItemShares { get; set; } = new List<ItemShare>();
    }
}
