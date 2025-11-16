namespace Common.DTO.Teacher.Request
{
    public class TeacherSearchRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public Guid? LanguageId { get; set; }
        public string? LanguageCode { get; set; }
        public string? ProficiencyCode { get; set; }
        public int? MinProficiencyOrder { get; set; }
        public int? MaxProficiencyOrder { get; set; }
        public double? MinRating { get; set; }
        public double? MaxRating { get; set; }
        public int? MinReviewCount { get; set; }
        public string? SortBy { get; set; } // "rating", "reviews", "proficiency", "name"
        public bool? SortDescending { get; set; }
    }
}
