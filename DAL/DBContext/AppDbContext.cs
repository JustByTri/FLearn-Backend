using Microsoft.EntityFrameworkCore;
using DAL.Models;
using System.Security.Cryptography;
using System.Text;

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
        public DbSet<RegistrationOtp> RegistrationOtps { get; set; }
        public DbSet<TempRegistration> TempRegistrations { get; set; }
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

            modelBuilder.Entity<RoadmapDetail>(entity =>
            {
                entity.HasIndex(e => new { e.RoadmapID, e.StepNumber }).IsUnique();
                entity.HasOne(e => e.Roadmap)
                      .WithMany(r => r.RoadmapDetails)
                      .HasForeignKey(e => e.RoadmapID)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            SeedData(modelBuilder);
        }
        private void SeedData(ModelBuilder modelBuilder)
        {
            var now = DateTime.UtcNow;

          
            var adminRoleId = Guid.NewGuid();
            var staffRoleId = Guid.NewGuid();
            var teacherRoleId = Guid.NewGuid();
            var learnerRoleId = Guid.NewGuid();

            var englishId = Guid.NewGuid();
            var japaneseId = Guid.NewGuid();
            var chineseId = Guid.NewGuid();

            var adminUserId = Guid.NewGuid();
            var staffEnUserId = Guid.NewGuid();
            var staffJpUserId = Guid.NewGuid();
            var staffZhUserId = Guid.NewGuid();

            var (adminHash, adminSalt) = CreatePasswordHash("Flearn@123");
            var (staffHash, staffSalt) = CreatePasswordHash("Staff@123");

           
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = adminRoleId, Name = "Admin", Description = "System administrator with full access", CreatedAt = now },
                new Role { RoleID = staffRoleId, Name = "Staff", Description = "Staff member for specific language support", CreatedAt = now },
                new Role { RoleID = teacherRoleId, Name = "Teacher", Description = "Teacher who can create and manage courses", CreatedAt = now },
                new Role { RoleID = learnerRoleId, Name = "Learner", Description = "Student learning languages", CreatedAt = now }
            );


            modelBuilder.Entity<Language>().HasData(
                new Language { LanguageID = englishId, LanguageName = "English", LanguageCode = "EN", CreateAt = now },
                new Language { LanguageID = japaneseId, LanguageName = "Japanese", LanguageCode = "JP", CreateAt = now },
                new Language { LanguageID = chineseId, LanguageName = "Chinese", LanguageCode = "ZH", CreateAt = now }
            );

        
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserID = adminUserId,
                    UserName = "Flearn",
                    Email = "admin@flearn.com",
                    PasswordHash = adminHash,
                    PasswordSalt = adminSalt,
                    JobTitle = "System Administrator",
                    Industry = "Education Technology",
                    Status = true,
                    CreatedAt = now,
                    UpdateAt = now,
                    LastAcessAt = now,
                    IsEmailConfirmed = true,
                    MfaEnabled = false,
                    StreakDays = 0,
                    Interests = "System Management",
                    ProfilePictureUrl = "",
                    BirthDate = new DateTime(1990, 1, 1)
                },
                new User
                {
                    UserID = staffEnUserId,
                    UserName = "StaffEN",
                    Email = "staff.english@flearn.com",
                    PasswordHash = staffHash,
                    PasswordSalt = staffSalt,
                    JobTitle = "English Language Staff",
                    Industry = "Education",
                    Status = true,
                    CreatedAt = now,
                    UpdateAt = now,
                    LastAcessAt = now,
                    IsEmailConfirmed = true,
                    MfaEnabled = false,
                    StreakDays = 0,
                    Interests = "English Language Support",
                    ProfilePictureUrl = "",
                    BirthDate = new DateTime(1992, 3, 15)
                },
                new User
                {
                    UserID = staffJpUserId,
                    UserName = "StaffJP",
                    Email = "staff.japanese@flearn.com",
                    PasswordHash = staffHash,
                    PasswordSalt = staffSalt,
                    JobTitle = "Japanese Language Staff",
                    Industry = "Education",
                    Status = true,
                    CreatedAt = now,
                    UpdateAt = now,
                    LastAcessAt = now,
                    IsEmailConfirmed = true,
                    MfaEnabled = false,
                    StreakDays = 0,
                    Interests = "Japanese Language Support",
                    ProfilePictureUrl = "",
                    BirthDate = new DateTime(1991, 7, 22)
                },
                new User
                {
                    UserID = staffZhUserId,
                    UserName = "StaffZH",
                    Email = "staff.chinese@flearn.com",
                    PasswordHash = staffHash,
                    PasswordSalt = staffSalt,
                    JobTitle = "Chinese Language Staff",
                    Industry = "Education",
                    Status = true,
                    CreatedAt = now,
                    UpdateAt = now,
                    LastAcessAt = now,
                    IsEmailConfirmed = true,
                    MfaEnabled = false,
                    StreakDays = 0,
                    Interests = "Chinese Language Support",
                    ProfilePictureUrl = "",
                    BirthDate = new DateTime(1993, 12, 8)
                }
            );

        
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = adminUserId, RoleID = adminRoleId },
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = staffEnUserId, RoleID = staffRoleId },
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = staffJpUserId, RoleID = staffRoleId },
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = staffZhUserId, RoleID = staffRoleId }
            );


            modelBuilder.Entity<UserLearningLanguage>().HasData(
                new UserLearningLanguage { UserLearningLanguageID = Guid.NewGuid(), UserID = staffEnUserId, LanguageID = englishId },
                new UserLearningLanguage { UserLearningLanguageID = Guid.NewGuid(), UserID = staffJpUserId, LanguageID = japaneseId },
                new UserLearningLanguage { UserLearningLanguageID = Guid.NewGuid(), UserID = staffZhUserId, LanguageID = chineseId }
            );

      
            var grammarTopicId = Guid.NewGuid();
            var vocabularyTopicId = Guid.NewGuid();
            var conversationTopicId = Guid.NewGuid();
            var pronunciationTopicId = Guid.NewGuid();

            modelBuilder.Entity<Topic>().HasData(
                new Topic { TopicID = grammarTopicId, Name = "Grammar", Description = "Basic and advanced grammar concepts" },
                new Topic { TopicID = vocabularyTopicId, Name = "Vocabulary", Description = "Essential vocabulary for daily communication" },
                new Topic { TopicID = conversationTopicId, Name = "Conversation", Description = "Practical conversation skills and dialogues" },
                new Topic { TopicID = pronunciationTopicId, Name = "Pronunciation", Description = "Pronunciation and speaking skills" }
            );

            // 7. Seed Achievements
            var firstLessonAchievementId = Guid.NewGuid();
            var firstCourseAchievementId = Guid.NewGuid();
            var streakAchievementId = Guid.NewGuid();

            modelBuilder.Entity<Achievement>().HasData(
                new Achievement
                {
                    AchievementID = firstLessonAchievementId,
                    Title = "First Steps",
                    Description = "Complete your first lesson",
                    IconUrl = "🎯",
                    Critertia = "Complete 1 lesson"
                },
                new Achievement
                {
                    AchievementID = firstCourseAchievementId,
                    Title = "Course Completion",
                    Description = "Complete your first course",
                    IconUrl = "🏆",
                    Critertia = "Complete 1 course"
                },
                new Achievement
                {
                    AchievementID = streakAchievementId,
                    Title = "Week Warrior",
                    Description = "Maintain a 7-day learning streak",
                    IconUrl = "🔥",
                    Critertia = "7 consecutive days of learning"
                }
            );
        }

        private (string hash, string salt) CreatePasswordHash(string password)
        {
            using (var hmac = new HMACSHA512())
            {
                var salt = Convert.ToBase64String(hmac.Key);
                var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
                return (hash, salt);
            }
        }
    }
}

