using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Course
    {
        [Key]
        public Guid CourseID { get; set; }
        [Required]
        public Guid TemplateId { get; set; }
        [ForeignKey(nameof(TemplateId))]
        public virtual CourseTemplate Template { get; set; }
        [Required]
        public Guid TeacherId { get; set; } // UserId + LanguageId
        [ForeignKey(nameof(TeacherId))]
        public virtual TeacherProfile Teacher { get; set; }
        public Guid? ApprovedByID { get; set; } // UserId of staff who approved + LanguageId
        [ForeignKey(nameof(ApprovedByID))]
        public virtual StaffLanguage? ApprovedBy { get; set; }
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        [Required]
        [StringLength(1000)]
        public string? Description { get; set; }
        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }
        [StringLength(200)]
        public string? PublicId { get; set; }
        [Required]
        [Range(0, 5_000_000)]
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public CourseType Type { get; set; } = CourseType.Paid;
        public LevelType Level { get; set; } = LevelType.Beginner;
        public int LearnerCount { get; set; } = 0;
        public float AverageRating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;
        public int NumLessons { get; set; } = 0;
        public CourseStatus Status { get; set; } = CourseStatus.Draft;
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<CourseTopic> CourseTopics { get; set; } = new List<CourseTopic>();
        public virtual ICollection<CourseGoal> CourseGoals { get; set; } = new List<CourseGoal>();
        public virtual ICollection<CourseUnit> CourseUnits { get; set; } = new List<CourseUnit>();
        public virtual ICollection<PurchaseDetail>? PurchasesDetails { get; set; } = new List<PurchaseDetail>();
        public virtual ICollection<Enrollment>? Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<CourseSubmission>? CourseSubmissions { get; set; } = new List<CourseSubmission>();
        public virtual ICollection<RoadmapDetail>? RoadmapDetails { get; set; } = new List<RoadmapDetail>();
        public virtual ICollection<Transaction>? Transactions { get; set; } = new List<Transaction>();
    }
}
