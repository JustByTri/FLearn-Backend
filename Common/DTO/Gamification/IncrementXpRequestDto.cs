using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Gamification
{
    public class IncrementXpRequestDto
    {
        [Range(1,1000)]
        public int Amount { get; set; }
        [MaxLength(100)]
        public string Source { get; set; } = string.Empty; // e.g. "exercise", "conversation", "lesson"
        public string? ReferenceId { get; set; } // optional entity id for idempotency/logging
    }
}
