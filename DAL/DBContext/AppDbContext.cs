using Microsoft.EntityFrameworkCore;
using DAL.Models;

namespace DAL.DBContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }
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
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Roadmap> Roadmaps { get; set; }
        public DbSet<RoadmapDetail> RoadmapDetails { get; set; }
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
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasOne(e => e.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Roadmap configuration
            modelBuilder.Entity<Roadmap>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Roadmaps)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Language)
                      .WithMany(l => l.Roadmaps)
                      .HasForeignKey(e => e.LanguageID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // RoadmapDetail configuration
            modelBuilder.Entity<RoadmapDetail>(entity =>
            {
                entity.HasIndex(e => new { e.RoadmapID, e.StepNumber }).IsUnique();
                entity.HasOne(e => e.Roadmap)
                      .WithMany(r => r.RoadmapDetails)
                      .HasForeignKey(e => e.RoadmapID)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
