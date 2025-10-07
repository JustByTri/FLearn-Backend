using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LanguageLevel
    {
        [Key]
        public Guid LanguageLevelID { get; set; }
        [Required]
        public Guid LanguageID { get; set; }
        [ForeignKey(nameof(LanguageID))]
        public virtual Language Language { get; set; }
        [Required]
        [StringLength(100)]
        public string LevelName { get; set; }
        public string? Description { get; set; }
        public int Position { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
