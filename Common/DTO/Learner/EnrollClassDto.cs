using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Learner
{
    public class EnrollClassDto
    {
        [Required(ErrorMessage = "Class ID là bắt buộc")]
        public Guid ClassID { get; set; }
    }

    public class ClassEnrollmentResponseDto
    {
        public Guid EnrollmentID { get; set; }
        public Guid ClassID { get; set; }
        public string ClassName { get; set; }
        public decimal AmountToPay { get; set; }
        public string PaymentUrl { get; set; }
        public string PaymentTransactionId { get; set; }
        public DateTime PaymentExpiry { get; set; }
        public string Status { get; set; }
    }
}
