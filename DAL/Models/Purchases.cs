using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Purchases
    {
        [Key]
        public Guid PurchasesID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        [Required]
        public DateTime PurchasedAt { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }

        public ICollection<PurchasesDetail> PurchasesDetails { get; set; }
    }
}
