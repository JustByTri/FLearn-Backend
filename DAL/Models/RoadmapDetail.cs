using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class RoadmapDetail
    {
        [Key]
        public Guid RoadmapDetailID { get; set; }

        [Required]
        public Guid RoadmapID { get; set; }

        [Required]
        public int StepNumber { get; set; }

        [Required]
        [StringLength(300)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [StringLength(500)]
        public string Skills { get; set; } 

        public string? Resources { get; set; } 

        [Required]
        public int EstimatedHours { get; set; }

        [Required]
        [StringLength(50)]
        public string DifficultyLevel { get; set; } 

        [Required]
        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

     
        [ForeignKey("RoadmapID")]
        public virtual Roadmap Roadmap { get; set; }
    }
}