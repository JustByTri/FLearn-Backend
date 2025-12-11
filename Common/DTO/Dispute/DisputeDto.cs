namespace Common.DTO.Dispute
{
    /// <summary>
    /// DTO tr? v? thông tin dispute
    /// </summary>
    public class DisputeDto
    {
        public Guid DisputeId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public Guid EnrollmentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AdminResponse { get; set; }
        public string? ResolvedByAdminName { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? ResolvedAt { get; set; }

        /// <summary>
        /// S? ti?n có th? ???c hoàn l?i (n?u dispute ???c ch?p nh?n)
        /// </summary>
        public decimal? PotentialRefundAmount { get; set; }
    }
}
