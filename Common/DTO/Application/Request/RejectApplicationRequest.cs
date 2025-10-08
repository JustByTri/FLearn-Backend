using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Application.Request
{
    public class RejectApplicationRequest
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; }
    }
}
