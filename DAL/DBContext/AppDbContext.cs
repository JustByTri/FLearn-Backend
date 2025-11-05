using DAL.Models;
using DAL.SeedData;
using Microsoft.EntityFrameworkCore;

namespace DAL.DBContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() { }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<ApplicationCertType> ApplicationCertTypes { get; set; }
        public DbSet<CertificateType> CertificateTypes { get; set; }
        public DbSet<ClassDispute> ClassDisputes { get; set; }
        public DbSet<ClassEnrollment> ClassEnrollments { get; set; }
        public DbSet<ConversationMessage> ConversationMessages { get; set; }
        public DbSet<ConversationSession> ConversationSessions { get; set; }
        public DbSet<ConversationTask> ConversationTasks { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseReview> CourseReviews { get; set; }
        public DbSet<CourseSubmission> CourseSubmissions { get; set; }
        public DbSet<CourseTemplate> CourseTemplates { get; set; }
        public DbSet<CourseTopic> CourseTopics { get; set; }
        public DbSet<CourseUnit> CourseUnits { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<ExerciseGradingAssignment> ExerciseGradingAssignments { get; set; }
        public DbSet<ExerciseSubmission> ExerciseSubmissions { get; set; }
        public DbSet<GlobalConversationPrompt> GlobalConversationPrompts { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<LanguageLevel> LanguageLevels { get; set; }
        public DbSet<LearnerAchievement> LearnerAchievements { get; set; }
        public DbSet<LearnerLanguage> LearnerLanguages { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LessonActivityLog> LessonActivityLogs { get; set; }
        public DbSet<LessonProgress> LessonProgresses { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<ManagerLanguage> ManagerLanguages { get; set; }
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<PayoutRequest> PayoutRequests { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Program> Programs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<RefundRequest> RefundRequests { get; set; }
        public DbSet<RegistrationOtp> RegistrationOtps { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<TeacherApplication> TeacherApplications { get; set; }
        public DbSet<TeacherBankAccount> TeacherBankAccounts { get; set; }
        public DbSet<TeacherClass> TeacherClasses { get; set; }
        public DbSet<TeacherEarningAllocation> TeacherEarningAllocations { get; set; }
        public DbSet<TeacherPayout> TeacherPayouts { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<TeacherProgramAssignment> TeacherProgramAssignments { get; set; }
        public DbSet<TeacherReview> TeacherReviews { get; set; }
        public DbSet<TempRegistration> TempRegistrations { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<UnitProgress> UnitProgresses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.UseCollation("utf8mb4_general_ci");

            modelBuilder.Entity<GlobalConversationPrompt>()
                .HasOne(p => p.CreatedByAdmin)
                .WithMany(u => u.CreatedPrompts)
                .HasForeignKey(p => p.CreatedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GlobalConversationPrompt>()
                .HasOne(p => p.LastModifiedByAdmin)
                .WithMany(u => u.ModifiedPrompts)
                .HasForeignKey(p => p.LastModifiedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

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
            //DbSeeder.SeedData(modelBuilder);
        }
    }
}

