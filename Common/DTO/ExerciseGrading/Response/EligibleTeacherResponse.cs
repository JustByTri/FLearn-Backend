namespace Common.DTO.ExerciseGrading.Response
{
    public class EligibleTeacherResponse
    {
        public Guid TeacherId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public string ProficiencyCode { get; set; } // Ví dụ: IELTS 8.0, N1, C2
        public int ProficiencyOrder { get; set; }

        // Thông số giúp Manager ra quyết định
        public double AverageRating { get; set; }
        public int ActiveAssignmentsCount { get; set; } // Giáo viên này đang chấm bao nhiêu bài? (tránh giao cho người đang quá tải)
        public bool IsRecommended { get; set; } // Gợi ý (ví dụ: ít bài, rating cao)
    }
}
