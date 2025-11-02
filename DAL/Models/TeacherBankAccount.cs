using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class TeacherBankAccount
    {
        [Key]
        public Guid BankAccountId { get; set; }
        [Required]
        public Guid TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public virtual TeacherProfile Teacher { get; set; } = null!;
        [Required, StringLength(100)]
        public string BankName { get; set; } = null!;
        [StringLength(100)]
        public string? BankBranch { get; set; }
        [Required, StringLength(50)]
        public string AccountNumber { get; set; } = null!;
        [Required, StringLength(100)]
        public string AccountHolder { get; set; } = null!;
        public bool IsDefault { get; set; } = false;
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? UpdatedAt { get; set; }
        public virtual ICollection<PayoutRequest> PayoutRequests { get; set; } = new List<PayoutRequest>();
    }
}
