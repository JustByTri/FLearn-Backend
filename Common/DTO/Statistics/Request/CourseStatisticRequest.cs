using DAL.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Statistics.Request
{
    public class CourseStatisticRequest
    {
        [Required]
        public Guid CourseId { get; set; }
        public int Year { get; set; } = TimeHelper.GetVietnamTime().Year;
    }
}
