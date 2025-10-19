using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Admin
{
    public class ResolveDisputeDto
    {
        [Required]
        public string Resolution { get; set; } = string.Empty; // "refund", "partial", "refuse"

        public decimal? RefundAmount { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        [StringLength(500)]
        public string? ReasonForResolution { get; set; }
    }
}
