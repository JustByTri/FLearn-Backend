namespace Common.DTO.Teacher.Response
{
    public class TeacherSearchResponse
    {
        public Guid TeacherId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public Guid LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string ProficiencyCode { get; set; } = string.Empty;
        public int ProficiencyOrder { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string MeetingUrl { get; set; } = string.Empty;
        public bool Status { get; set; }
        public string? CreatedAt { get; set; }
        public int TotalGradedAssignments { get; set; }
        public int ActiveAssignments { get; set; }
    }
}
