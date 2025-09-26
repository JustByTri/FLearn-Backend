using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ExerciseOption
    {
        [Key]
        public Guid OptionID { get; set; }
        [Required]
        [StringLength(500)]
        public required string Text { get; set; }
        public bool IsCorrect { get; set; } = false;
        [Required]
        public Guid ExerciseID { get; set; }
        [ForeignKey(nameof(ExerciseID))]
        public Exercise? Exercise { get; set; }
        [Range(1, 100)]
        public int Position { get; set; }
    }
}
