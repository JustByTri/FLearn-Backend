using System;
namespace Common.DTO.Teacher
{
    public class ProgramStatsDto
    {
        public Guid ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public int ClassCount { get; set; }
        public int StudentCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
