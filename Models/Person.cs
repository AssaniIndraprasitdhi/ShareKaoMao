using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShareKaoMao.Models
{
    public class Person
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string Instagram { get; set; }

        public int BillId { get; set; }
        public Bill Bill { get; set; }

        public List<ItemShare> ItemShares { get; set; } = new List<ItemShare>();
    }
}
