using System;
using System.Collections.Generic;
using Common.DTO.PayOut;
namespace Common.DTO.Teacher
{
    public class TeacherDashboardDto
    {
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalPayout { get; set; }
        public int PendingPayouts { get; set; }
        public int CompletedPayouts { get; set; }
        public int CancelledPayouts { get; set; }
        public int ActiveClasses { get; set; }
        public int FinishedClasses { get; set; }
        public List<ClassSummaryDto> Classes { get; set; } = new();
        public List<PayoutSummaryDto> Payouts { get; set; } = new();
        public List<ProgramStatsDto> ProgramStats { get; set; } = new();
        public List<PeriodStatsDto> PeriodStats { get; set; } = new();
    }
}
