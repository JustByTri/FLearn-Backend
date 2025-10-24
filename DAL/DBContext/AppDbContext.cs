using DAL.Helpers;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.DBContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() { }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<AIFeedBack> AIFeedBacks { get; set; }
        public DbSet<ApplicationCertType> ApplicationCertTypes { get; set; }
        public DbSet<CertificateType> CertificateTypes { get; set; }
        public DbSet<ContentIssueReport> ContentIssueReports { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseGoal> CourseGoals { get; set; }
        public DbSet<CourseReview> CourseReviews { get; set; }
        public DbSet<CourseSubmission> CourseSubmissions { get; set; }
        public DbSet<CourseTemplate> CourseTemplates { get; set; }
        public DbSet<CourseTopic> CourseTopics { get; set; }
        public DbSet<CourseUnit> CourseUnits { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<ExerciseEvaluationDetail> ExerciseEvaluationDetails { get; set; }
        public DbSet<ExerciseSubmission> ExerciseSubmissions { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<LanguageLevel> LanguageLevels { get; set; }
        public DbSet<LearnerAchievement> LearnerAchievements { get; set; }
        public DbSet<LearnerLanguage> LearnerLanguages { get; set; }
        public DbSet<LearnerProgress> LearnerProgresses { get; set; }
        public DbSet<LearnerGoal> LearnerGoals { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LessonActivityLog> LessonActivityLogs { get; set; }
        public DbSet<LessonBooking> LessonBookings { get; set; }
        public DbSet<LessonDispute> LessonDisputes { get; set; }
        public DbSet<LessonReview> LessonReviews { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseDetail> PurchaseDetails { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<RegistrationOtp> RegistrationOtps { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Roadmap> Roadmaps { get; set; }
        public DbSet<RoadmapDetail> RoadmapDetails { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<StaffLanguage> StaffLanguages { get; set; }
        public DbSet<TeacherApplication> TeacherApplications { get; set; }
        public DbSet<TeacherPayout> TeacherPayouts { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<TeacherReview> TeacherReviews { get; set; }
        public DbSet<TempRegistration> TempRegistrations { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Transaction> UserTransactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<GlobalConversationPrompt> GlobalConversationPrompts { get; set; }
        public DbSet<ConversationMessage> ConversationMessages { get; set; }
        public DbSet<ConversationSession> ConversationSession { get; set; }
        public DbSet<ConversationTask> ConversationTasks { get; set; }
        public DbSet<TeacherClass> TeacherClasses { get; set; }
        public DbSet<ClassEnrollment> ClassEnrollments { get; set; }
        public DbSet<ClassDispute> ClassDisputes { get; set; }
        public DbSet<RefundRequest> RefundRequests { get; set; }


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

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var now = TimeHelper.GetVietnamTime();

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

            var (adminHash, adminSalt) = PasswordHelper.CreatePasswordHash("Flearn@123");
            var (staffHash, staffSalt) = PasswordHelper.CreatePasswordHash("Staff@123");

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = adminRoleId, Name = "Admin", Description = "System administrator with full access", CreatedAt = now },
                new Role { RoleID = staffRoleId, Name = "Staff", Description = "Staff member for specific language support", CreatedAt = now },
                new Role { RoleID = teacherRoleId, Name = "Teacher", Description = "Teacher who can create and manage courses", CreatedAt = now },
                new Role { RoleID = learnerRoleId, Name = "Learner", Description = "Student learning languages", CreatedAt = now });

            modelBuilder.Entity<Goal>().HasData(
               new Goal
               {
                   Id = 1,
                   Name = "Giao tiếp hàng ngày",
                   Description = "Học để có thể trò chuyện cơ bản trong các tình huống hàng ngày như chào hỏi, hỏi đường, mua sắm, gọi món ăn.",
                   CreatedAt = now,
                   UpdatedAt = now,
                   Status = true
               },
               new Goal
               {
                   Id = 2,
                   Name = "Du học hoặc làm việc ở nước ngoài",
                   Description = "Đạt trình độ ngôn ngữ đủ để học tập, làm việc hoặc sinh sống ở nước ngoài.",
                   CreatedAt = now,
                   UpdatedAt = now,
                   Status = true
               },
               new Goal
               {
                   Id = 3,
                   Name = "Thi chứng chỉ ngoại ngữ",
                   Description = "Chuẩn bị cho các kỳ thi như IELTS, TOEFL, JLPT, TOPIK, DELF hoặc HSK.",
                   CreatedAt = now,
                   UpdatedAt = now,
                   Status = true
               },
               new Goal
               {
                   Id = 4,
                   Name = "Giao tiếp công việc",
                   Description = "Rèn luyện kỹ năng viết email, tham gia họp, thuyết trình và thương lượng bằng ngoại ngữ.",
                   CreatedAt = now,
                   UpdatedAt = now,
                   Status = true
               },
               new Goal
               {
                   Id = 5,
                   Name = "Tăng cường kỹ năng nghe – nói",
                   Description = "Tập trung cải thiện khả năng nghe hiểu và phản xạ nói trong các tình huống thực tế.",
                   CreatedAt = now,
                   UpdatedAt = now,
                   Status = true
               },
               new Goal
               {
                   Id = 6,
                   Name = "Đọc hiểu tài liệu chuyên ngành",
                   Description = "Phục vụ cho học tập và công việc trong các lĩnh vực chuyên môn như CNTT, kinh doanh, y học, v.v.",
                   CreatedAt = now,
                   UpdatedAt = now,
                   Status = true
               }
               );

            modelBuilder.Entity<CertificateType>().HasData(
    // English certificates
    new CertificateType
    {
        CertificateTypeId = Guid.NewGuid(),
        LanguageId = englishId,
        Name = "IELTS",
        Description = "International English Language Testing System",
        Status = true,
        CreatedAt = now,
        UpdatedAt = now
    },
    new CertificateType
    {
        CertificateTypeId = Guid.NewGuid(),
        LanguageId = englishId,
        Name = "TOEFL",
        Description = "Test of English as a Foreign Language",
        Status = true,
        CreatedAt = now,
        UpdatedAt = now
    },
    new CertificateType
    {
        CertificateTypeId = Guid.NewGuid(),
        LanguageId = englishId,
        Name = "Cambridge English",
        Description = "Cambridge Assessment English certifications (KET, PET, FCE, CAE, CPE)",
        Status = true,
        CreatedAt = now,
        UpdatedAt = now
    },
     new CertificateType
     {
         CertificateTypeId = Guid.NewGuid(),
         LanguageId = englishId,
         Name = "TOEIC",
         Description = "Test of English for International Communication",
         Status = true,
         CreatedAt = now,
         UpdatedAt = now
     },
     new CertificateType
     {
         CertificateTypeId = Guid.NewGuid(),
         LanguageId = englishId,
         Name = "Duolingo English Test",
         Description = "Online English proficiency test accepted by many universities",
         Status = true,
         CreatedAt = now,
         UpdatedAt = now
     },
    // Japanese certificates
    new CertificateType
    {
        CertificateTypeId = Guid.NewGuid(),
        LanguageId = japaneseId,
        Name = "JLPT",
        Description = "Japanese Language Proficiency Test (N1–N5)",
        Status = true,
        CreatedAt = now,
        UpdatedAt = now
    },
    new CertificateType
    {
        CertificateTypeId = Guid.NewGuid(),
        LanguageId = japaneseId,
        Name = "BJT",
        Description = "Business Japanese Proficiency Test",
        Status = true,
        CreatedAt = now,
        UpdatedAt = now
    },
    new CertificateType
    {
        CertificateTypeId = Guid.NewGuid(),
        LanguageId = japaneseId,
        Name = "J-Test",
        Description = "Practical Japanese Test (A–F levels) focusing on real communication",
        Status = true,
        CreatedAt = now,
        UpdatedAt = now
    },
    // Chinese certificates
    new CertificateType
    {
        CertificateTypeId = Guid.NewGuid(),
        LanguageId = chineseId,
        Name = "HSK",
        Description = "Hanyu Shuiping Kaoshi – Chinese Proficiency Test (Level 1–6)",
        Status = true,
        CreatedAt = now,
        UpdatedAt = now
    },
    new CertificateType
    {
        CertificateTypeId = Guid.NewGuid(),
        LanguageId = chineseId,
        Name = "HSKK",
        Description = "Hanyu Shuiping Kouyu Kaoshi – Chinese Proficiency Speaking Test (Beginner to Advanced)",
        Status = true,
        CreatedAt = now,
        UpdatedAt = now
    },
    new CertificateType
    {
        CertificateTypeId = Guid.NewGuid(),
        LanguageId = chineseId,
        Name = "TOCFL",
        Description = "Test of Chinese as a Foreign Language – for traditional Chinese learners",
        Status = true,
        CreatedAt = now,
        UpdatedAt = now
    }
);


            modelBuilder.Entity<Language>().HasData(
                 new Language { LanguageID = englishId, LanguageName = "English", LanguageCode = "EN", CreatedAt = now },
                 new Language { LanguageID = japaneseId, LanguageName = "Japanese", LanguageCode = "JA", CreatedAt = now },
                 new Language { LanguageID = chineseId, LanguageName = "Chinese", LanguageCode = "ZH", CreatedAt = now }
                 );
            // ===== Seed Data for LanguageLevel =====
            var languageLevels = new List<LanguageLevel>
            {
    // ===== English CEFR =====
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = englishId,
        LevelName = "A1",
        Description = "Hiểu và dùng các cụm từ/câu cơ bản cho nhu cầu giao tiếp thiết yếu.",
        Position = 1,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = englishId,
        LevelName = "A2",
        Description = "Giao tiếp tình huống đơn giản, quen thuộc.",
        Position = 2,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = englishId,
        LevelName = "B1",
        Description = "Xử lý tình huống du lịch, nói/viết về chủ đề quen thuộc.",
        Position = 3,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = englishId,
        LevelName = "B2",
        Description = "Giao tiếp trôi chảy tự nhiên, hiểu văn bản chi tiết (cấp độ thường dùng cho quốc tế).",
        Position = 4,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = englishId,
        LevelName = "C1",
        Description = "Sử dụng linh hoạt cho mục đích học thuật/chuyên môn, tạo văn bản phức tạp.",
        Position = 5,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = englishId,
        LevelName = "C2",
        Description = "Gần như người bản xứ, hiểu mọi thứ và diễn đạt tinh tế trong mọi tình huống.",
        Position = 6,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },

    // ===== Japanese JLPT =====
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = japaneseId,
        LevelName = "N5",
        Description = "Sơ cấp thấp. Hiểu từ vựng, ngữ pháp rất cơ bản. Chỉ giao tiếp được các câu chào hỏi, giới thiệu đơn giản.",
        Position = 1,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = japaneseId,
        LevelName = "N4",
        Description = "Sơ cấp. Hiểu được tiếng Nhật căn bản dùng trong tình huống thường ngày.",
        Position = 2,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = japaneseId,
        LevelName = "N3",
        Description = "Trung cấp. Cầu nối giữa sơ cấp và cao cấp. Có thể tham gia vào các cuộc trò chuyện hàng ngày tự nhiên hơn.",
        Position = 3,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = japaneseId,
        LevelName = "N2",
        Description = "Trung – Cao cấp. Có khả năng sử dụng tiếng Nhật trong công việc, học tập và hầu hết các tình huống.",
        Position = 4,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = japaneseId,
        LevelName = "N1",
        Description = "Cao cấp. Hiểu được các tài liệu phức tạp, trừu tượng, và sử dụng ngôn ngữ chính xác, lưu loát.",
        Position = 5,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },

    // ===== Chinese HSK =====
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = chineseId,
        LevelName = "HSK 1",
        Description = "Rất cơ bản. Hiểu và sử dụng được các cụm từ đơn giản, đáp ứng nhu cầu giao tiếp cụ thể.",
        Position = 1,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = chineseId,
        LevelName = "HSK 2",
        Description = "Hiểu được các câu đơn giản về chủ đề quen thuộc và có thể trao đổi thông tin trực tiếp.",
        Position = 2,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = chineseId,
        LevelName = "HSK 3",
        Description = "Có thể giao tiếp bằng tiếng Trung trong các tình huống cơ bản của cuộc sống, học tập và công việc.",
        Position = 3,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = chineseId,
        LevelName = "HSK 4",
        Description = "Có thể thảo luận về nhiều chủ đề, diễn đạt ý kiến trôi chảy và hiểu văn bản phức tạp hơn.",
        Position = 4,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = chineseId,
        LevelName = "HSK 5",
        Description = "Có khả năng đọc báo, xem phim, và diễn thuyết bằng tiếng Trung. Đủ cho học tập chuyên sâu.",
        Position = 5,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new LanguageLevel
    {
        LanguageLevelID = Guid.NewGuid(),
        LanguageID = chineseId,
        LevelName = "HSK 6",
        Description = "Có thể hiểu mọi thông tin nghe hoặc đọc, diễn đạt ý kiến lưu loát, chính xác và tinh tế gần như người bản xứ.",
        Position = 6,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    }
};
            modelBuilder.Entity<LanguageLevel>().HasData(languageLevels);

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserID = adminUserId,
                    UserName = "Admin",
                    Email = "admin@flearn.com",
                    PasswordHash = adminHash,
                    PasswordSalt = adminSalt,
                    IsEmailConfirmed = true,
                    BirthDate = new DateTime(1990, 1, 1),
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new User
                {
                    UserID = staffEnUserId,
                    UserName = "StaffEN",
                    Email = "staff.english@flearn.com",
                    PasswordHash = staffHash,
                    PasswordSalt = staffSalt,
                    IsEmailConfirmed = true,
                    BirthDate = new DateTime(1992, 3, 15),
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new User
                {
                    UserID = staffJpUserId,
                    UserName = "StaffJA",
                    Email = "staff.japanese@flearn.com",
                    PasswordHash = staffHash,
                    PasswordSalt = staffSalt,
                    IsEmailConfirmed = true,
                    BirthDate = new DateTime(1991, 7, 22),
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new User
                {
                    UserID = staffZhUserId,
                    UserName = "StaffZH",
                    Email = "staff.chinese@flearn.com",
                    PasswordHash = staffHash,
                    PasswordSalt = staffSalt,
                    IsEmailConfirmed = true,
                    BirthDate = new DateTime(1993, 12, 8),
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );


            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = adminUserId, RoleID = adminRoleId },
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = staffEnUserId, RoleID = staffRoleId },
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = staffJpUserId, RoleID = staffRoleId },
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = staffZhUserId, RoleID = staffRoleId }
            );

            modelBuilder.Entity<StaffLanguage>().HasData(
                new StaffLanguage { StaffLanguageId = Guid.NewGuid(), UserId = staffEnUserId, LanguageId = englishId, CreatedAt = now, UpdatedAt = now },
                new StaffLanguage { StaffLanguageId = Guid.NewGuid(), UserId = staffJpUserId, LanguageId = japaneseId, CreatedAt = now, UpdatedAt = now },
                new StaffLanguage { StaffLanguageId = Guid.NewGuid(), UserId = staffZhUserId, LanguageId = chineseId, CreatedAt = now, UpdatedAt = now }
            );

            modelBuilder.Entity<Topic>().HasData(
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Chào hỏi và giới thiệu bản thân",
                    Description = "Các mẫu câu và từ vựng cơ bản để chào hỏi, giới thiệu tên, tuổi, nghề nghiệp, quốc tịch.",
                    ImageUrl = "https://example.com/images/greetings.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Du lịch và phương tiện di chuyển",
                    Description = "Từ vựng và hội thoại về đặt vé, hỏi đường, đi taxi, sân bay và khách sạn.",
                    ImageUrl = "https://example.com/images/travel.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Mua sắm và tiền bạc",
                    Description = "Tình huống giao tiếp khi mua hàng, hỏi giá, mặc cả, và thanh toán.",
                    ImageUrl = "https://example.com/images/shopping.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Ẩm thực và nhà hàng",
                    Description = "Gọi món, hỏi về món ăn, và nói chuyện với nhân viên phục vụ.",
                    ImageUrl = "https://example.com/images/restaurant.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Công việc và phỏng vấn",
                    Description = "Giao tiếp nơi công sở, viết email, và trả lời phỏng vấn xin việc.",
                    ImageUrl = "https://example.com/images/work.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Sở thích và thời gian rảnh",
                    Description = "Học cách nói về sở thích, thói quen, thể thao và các hoạt động giải trí.",
                    ImageUrl = "https://example.com/images/hobby.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Gia đình và bạn bè",
                    Description = "Từ vựng và câu giao tiếp nói về các mối quan hệ cá nhân.",
                    ImageUrl = "https://example.com/images/family.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Trường học và học tập",
                    Description = "Các chủ đề về lớp học, bài tập, giáo viên, kỳ thi và kết quả học tập.",
                    ImageUrl = "https://example.com/images/school.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Sức khỏe và bệnh viện",
                    Description = "Cách nói về triệu chứng, đi khám bệnh, thuốc và lời khuyên sức khỏe.",
                    ImageUrl = "https://example.com/images/health.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Thời tiết và thiên nhiên",
                    Description = "Miêu tả thời tiết, mùa, và các hiện tượng tự nhiên.",
                    ImageUrl = "https://example.com/images/weather.jpg",
                    Status = true
                }
            );
            modelBuilder.Entity<Achievement>().HasData(
                new Achievement
                {
                    AchievementID = Guid.NewGuid(),
                    LanguageId = englishId,
                    Title = "Bắt đầu hành trình học tiếng Anh",
                    Description = "Hoàn thành bài học đầu tiên của bạn và bắt đầu hành trình chinh phục tiếng Anh!",
                    IconUrl = "https://example.com/icons/start.png",
                    Criteria = "Hoàn thành khóa học đầu tiên trong tiếng Anh.",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Status = true
                },
                new Achievement
                {
                    AchievementID = Guid.NewGuid(),
                    LanguageId = englishId,
                    Title = "Chuyên gia giao tiếp cơ bản",
                    Description = "Thành thạo các chủ đề giao tiếp hàng ngày bằng tiếng Anh.",
                    IconUrl = "https://example.com/icons/communication.png",
                    Criteria = "Hoàn thành tất cả các bài học trong chủ đề Giao tiếp hàng ngày.",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Status = true
                },
                new Achievement
                {
                    AchievementID = Guid.NewGuid(),
                    LanguageId = englishId,
                    Title = "Liên tục 7 ngày học tập",
                    Description = "Giữ streak học tập liên tục trong 7 ngày.",
                    IconUrl = "https://example.com/icons/streak7.png",
                    Criteria = "Học ít nhất 1 bài mỗi ngày trong 7 ngày liên tiếp.",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Status = true
                },
                new Achievement
                {
                    AchievementID = Guid.NewGuid(),
                    LanguageId = japaneseId,
                    Title = "Chinh phục Hiragana",
                    Description = "Hoàn thành toàn bộ bảng chữ Hiragana cơ bản.",
                    IconUrl = "https://example.com/icons/hiragana.png",
                    Criteria = "Đạt điểm tối đa trong bài kiểm tra Hiragana.",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Status = true
                },
                new Achievement
                {
                    AchievementID = Guid.NewGuid(),
                    LanguageId = japaneseId,
                    Title = "Khám phá văn hóa Nhật Bản",
                    Description = "Hoàn thành tất cả các chủ đề về văn hóa, du lịch và ẩm thực Nhật Bản.",
                    IconUrl = "https://example.com/icons/japan.png",
                    Criteria = "Hoàn thành các topic có tag 'Culture' hoặc 'Travel'.",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Status = true
                },
                new Achievement
                {
                    AchievementID = Guid.NewGuid(),
                    LanguageId = chineseId,
                    Title = "500 từ vựng đầu tiên",
                    Description = "Ghi nhớ và sử dụng 500 từ tiếng Trung thông dụng nhất.",
                    IconUrl = "https://example.com/icons/vocabulary500.png",
                    Criteria = "Learn500Words",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Status = true
                },
                new Achievement
                {
                    AchievementID = Guid.NewGuid(),
                    LanguageId = chineseId,
                    Title = "Đọc hiểu sơ cấp HSK1",
                    Description = "Đạt trình độ đọc hiểu tương đương HSK1.",
                    IconUrl = "https://example.com/icons/hsk1.png",
                    Criteria = "PassHSK1",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Status = true
                }
            );
        }
    }
}

