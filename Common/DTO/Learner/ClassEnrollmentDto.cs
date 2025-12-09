using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Learner
{
    public class ClassEnrollmentDto
    {
        public Guid EnrollmentID { get; set; }
        public Guid ClassID { get; set; }
        public Guid StudentID { get; set; }

        /// <summary>
        /// Username của học viên (dùng để hiển thị)
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        public string StudentEmail { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public string? PaymentTransactionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
