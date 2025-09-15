using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Achievement
    {
        [Key]
        public Guid AchievementID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
        public string? IconUrl { get; set; }
        public string? Critertia { get; set; }
    }
}
