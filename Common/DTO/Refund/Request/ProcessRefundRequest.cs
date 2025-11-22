using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Refund.Request
{
    public class ProcessRefundRequest
    {
        [Required]
        public Guid RefundRequestId { get; set; }
        [Required]
        public bool isApproved { get; set; }
        [Required]
        public string Note { get; set; }
    }
}
