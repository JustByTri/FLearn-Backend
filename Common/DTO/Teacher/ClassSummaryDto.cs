using System;
namespace Common.DTO.Teacher
{
    public class ClassSummaryDto
    {
        public Guid ClassID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int StudentCount { get; set; }
        public decimal Revenue { get; set; }
        public Guid ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
    }
}
