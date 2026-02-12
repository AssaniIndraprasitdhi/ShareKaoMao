using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShareKaoMao.Models
{
    public class Bill
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public DateTime CreatedAt { get; set; }

        public decimal VatPercent { get; set; }

        public decimal ServicePercent { get; set; }

        public decimal TipAmount { get; set; }

        public RoundingMode RoundingMode { get; set; }

        public List<Person> People { get; set; } = new List<Person>();

        public List<Item> Items { get; set; } = new List<Item>();
    }
}
