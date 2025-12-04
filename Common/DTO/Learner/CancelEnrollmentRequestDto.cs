using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Learner
{
    public class CancelEnrollmentRequestDto
    {
        [StringLength(500, ErrorMessage = "Lý do không ???c v??t quá 500 ký t?")]
        public string? Reason { get; set; }
    }
}
