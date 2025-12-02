namespace Common.DTO.Admin
{
    /// <summary>
    /// Response DTO cho danh sách yêu c?u h?y l?p (Manager view)
    /// </summary>
    public class ClassCancellationRequestDto
    {
        public Guid CancellationRequestId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public DateTime ClassStartDateTime { get; set; }
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherEmail { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ManagerNote { get; set; }
        public string? ProcessedByManagerName { get; set; }
        public string RequestedAt { get; set; } = string.Empty;
        public string? ProcessedAt { get; set; }
        public int EnrolledStudentsCount { get; set; }
        public decimal TotalRefundAmount { get; set; }
    }
}
