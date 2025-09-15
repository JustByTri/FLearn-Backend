using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class PurchasesDetail
    {
        [Key]
        public Guid PurchasesDetailID { get; set; }

        [Required]
        public Guid PurchasesID { get; set; }

        [Required]
        public Guid CourseID { get; set; }

        [Required]
        [Range(0, 100000)]
        public decimal Amount { get; set; }
    }
}
