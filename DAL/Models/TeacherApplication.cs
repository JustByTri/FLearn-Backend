using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class TeacherApplication
    {
        [Key]
        public Guid TeacherApplicationID { get; set; }

        [Required]
        public Guid UserID { get; set; }
        public User? User { get; set; }

        [Required]
        [StringLength(1000)]
        public string Motivation { get; set; }

        [Required]
        public DateTime AppliedAt { get; set; }
        public DateTime SubmitAt { get; set; }
        public DateTime ReviewAt { get; set; }
        public Guid ReviewedBy { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public bool Status { get; set; }
        public Guid TeacherCredentialID { get; set; }
        public ICollection<TeacherCredential>? Credentials { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
