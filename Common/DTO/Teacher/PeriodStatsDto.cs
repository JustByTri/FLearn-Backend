using System;
namespace Common.DTO.Teacher
{
    public class PeriodStatsDto
    {
        public string Period { get; set; } = string.Empty; // yyyy-MM or yyyy-MM-dd
        public int ClassCount { get; set; }
        public int StudentCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
