using DAL.Helpers;
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
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; } = null!;
        [Required]
        public Guid ProgramId { get; set; }
        [ForeignKey(nameof(ProgramId))]
        public virtual Program Program { get; set; } = null!;
        [Required]
        public Guid LevelId { get; set; }
        [ForeignKey(nameof(LevelId))]
        public virtual Level Level { get; set; } = null!;
        [Required]
        public Guid TemplateId { get; set; }
        [ForeignKey(nameof(TemplateId))]
        public virtual CourseTemplate Template { get; set; } = null!;
        [Required]
        public Guid TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public virtual TeacherProfile Teacher { get; set; } = null!;
        public Guid? ApprovedByID { get; set; }
        [ForeignKey(nameof(ApprovedByID))]
        public virtual ManagerLanguage ApprovedBy { get; set; } = null!;
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;
        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = null!;
        [Required]
        [StringLength(1000)]
        public string LearningOutcome { get; set; } = null!;
        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = null!;
        [StringLength(200)]
        public string PublicId { get; set; } = null!;
        [Required]
        [Range(0, 5_000_000)]
        public decimal Price { get; set; }
        [Range(0, 5_000_000)]
        public decimal? DiscountPrice { get; set; }
        [Required]
        public CourseType Type { get; set; }
        [Required]
        public int LearnerCount { get; set; } = 0;
        [Required]
        public double AverageRating { get; set; } = 0;
        [Required]
        public int ReviewCount { get; set; } = 0;
        [Required]
        public int NumLessons { get; set; } = 0;
        [Required]
        public int NumUnits { get; set; } = 0;
        [Required]
        public CourseStatus Status { get; set; } = CourseStatus.Draft;
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public virtual ICollection<CourseTopic> CourseTopics { get; set; } = new List<CourseTopic>();
        public virtual ICollection<CourseUnit> CourseUnits { get; set; } = new List<CourseUnit>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<CourseSubmission> CourseSubmissions { get; set; } = new List<CourseSubmission>();
        public virtual ICollection<CourseReview> CourseReviews { get; set; } = new List<CourseReview>();
    }
}
