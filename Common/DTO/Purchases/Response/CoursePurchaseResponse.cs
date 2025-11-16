using DAL.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Purchases.Response
{
    public class CoursePurchaseResponse
    {
        public Guid PurchaseId { get; set; }
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string CourseDescription { get; set; } = string.Empty;
        public string CourseThumbnail { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CreatedAt { get; set; }
        public string? PaidAt { get; set; }
        public string? StartsAt { get; set; }
        public string? ExpiresAt { get; set; }
        public string? EligibleForRefundUntil { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsRefundEligible { get; set; }
        public bool IsActive { get; set; }
        public Guid? EnrollmentId { get; set; }
        public string EnrollmentStatus { get; set; } = string.Empty;
        public CourseDetailResponse? CourseDetails { get; set; }
    }
    public class CourseDetailResponse
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public string? CourseType { get; set; }
        public string? GradingType { get; set; }
        public int NumLessons { get; set; }
        public int NumUnits { get; set; }
        public int DurationDays { get; set; }
        public int EstimatedHours { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int LearnerCount { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherAvatar { get; set; } = string.Empty;
    }
}
