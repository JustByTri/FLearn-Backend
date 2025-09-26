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
        [StringLength(200)]
        public required string Title { get; set; }
        [Required]
        [StringLength(1000)]
        public string? Description { get; set; }
        [StringLength(500)]
        public required string ImageUrl { get; set; }
        [StringLength(200)]
        public string? PublicId { get; set; }
        [Required]
        public Guid TemplateId { get; set; }
        [ForeignKey(nameof(TemplateId))]
        public CourseTemplate CourseTemplate { get; set; }
        [Required]
        [Range(0, 100000)]
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public CourseType CourseType { get; set; }
        [Required]
        public Guid TeacherID { get; set; }
        [ForeignKey(nameof(TeacherID))]
        public User Teacher { get; set; }
        [Required]
        public Guid LanguageID { get; set; }
        [ForeignKey(nameof(LanguageID))]
        public Language Language { get; set; }
        [Required]
        public int GoalId { get; set; }
        [ForeignKey(nameof(GoalId))]
        public Goal Goal { get; set; }
        [StringLength(50)]
        public string? Level { get; set; }
        public required LevelType CourseLevel { get; set; }
        [StringLength(100)]
        public string? SkillFocus { get; set; }
        public required SkillType CourseSkill { get; set; }
        public DateTime? PublishedAt { get; set; }
        [Range(0, 100)]
        public int NumLessons { get; } = 0;
        public Guid? ApprovedByID { get; set; }
        [ForeignKey(nameof(ApprovedByID))]
        public User? Staff { get; set; }
        public CourseStatus Status { get; set; } = CourseStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<CourseUnit> CourseUnits { get; set; } = new List<CourseUnit>();
        public virtual ICollection<CourseTopic> CourseTopics { get; set; } = new List<CourseTopic>();
        public virtual ICollection<PurchasesDetail> PurchasesDetails { get; set; } = new List<PurchasesDetail>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<CourseSubmission> CourseSubmissions { get; set; } = new List<CourseSubmission>();

    }
}
