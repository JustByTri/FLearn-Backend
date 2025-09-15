using Microsoft.EntityFrameworkCore;
using DAL.Models;

namespace DAL.DBContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<UserLearningLanguage> UserLearningLanguages { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseUnit> CourseUnits { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Purchases> Purchases { get; set; }
        public DbSet<PurchasesDetail> PurchasesDetails { get; set; }
        public DbSet<CourseTopic> CourseTopics { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<CourseSubmission> CourseSubmissions { get; set; }
        public DbSet<TeacherApplication> TeacherApplications { get; set; }
        public DbSet<TeacherCredential> TeacherCredentials { get; set; }
        public DbSet<Recording> Recordings { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<AIFeedBack> AIFeedBacks { get; set; }
        public DbSet<Conversation> Conversations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

           
            modelBuilder.UseCollation("utf8mb4_general_ci");

          
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(Guid) || property.ClrType == typeof(Guid?))
                    {
                        property.SetColumnType("char(36)");
                        property.SetCollation("utf8mb4_general_ci");
                    }
                }
            }
        }
    }
}
