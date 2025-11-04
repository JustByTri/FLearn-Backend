namespace Common.DTO.Teacher.Response
{
    public class TeacherProfileResponse
    {
        public Guid TeacherId { get; set; }
        public string? Language { get; set; }
        public string? FullName { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProficiencyCode { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string? MeetingUrl { get; set; }
    }
}
