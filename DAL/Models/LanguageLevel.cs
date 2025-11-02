using DAL.Helpers;
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
        public string LevelName { get; set; } // e.g., A1, A2, B1, B2, C1, C2, N5, N4, N3, N2, N1, etc.
        public string? Description { get; set; }
        public int OrderIndex { get; set; } // Order of the level within the language; example: A1=1, A2=2, B1=3, etc.
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
    }
}
