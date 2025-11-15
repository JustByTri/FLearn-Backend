using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LearnerXpEvent
    {
        [Key]
        public Guid LearnerXpEventId { get; set; }
        [Required]
        public Guid LearnerLanguageId { get; set; }
        [ForeignKey(nameof(LearnerLanguageId))]
        public virtual LearnerLanguage LearnerLanguage { get; set; } = null!;
        [Range(1,int.MaxValue)]
        public int Amount { get; set; }
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
