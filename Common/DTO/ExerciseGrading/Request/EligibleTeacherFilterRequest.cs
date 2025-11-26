using Common.DTO.Paging.Request;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.ExerciseGrading.Request
{
    public class EligibleTeacherFilterRequest : PagingRequest
    {
        [Required]
        public Guid ExerciseSubmissionId { get; set; }
    }
}
