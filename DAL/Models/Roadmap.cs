using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Roadmap
    {
        [Key]
        public Guid RoadmapID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        [Required]
        public Guid LanguageID { get; set; }

        [Required]
        [StringLength(300)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [StringLength(50)]
        public string CurrentLevel { get; set; }

        [Required]
        [StringLength(50)]
        public string TargetLevel { get; set; }

        [Required]
        public int EstimatedDuration { get; set; }

        [Required]
        [StringLength(20)]
        public string DurationUnit { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Progress { get; set; } 

      
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("LanguageID")]
        public virtual Language Language { get; set; }

        public virtual ICollection<RoadmapDetail> RoadmapDetails { get; set; }
    }
}
