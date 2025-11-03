using DAL.Helpers;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.SeedData
{
    public static class DbSeeder
    {
        public static void SeedData(ModelBuilder modelBuilder)
        {
            var now = TimeHelper.GetVietnamTime();

            // Tạo các Guid cố định cho từng ngôn ngữ
            var englishId = Guid.NewGuid();
            var japaneseId = Guid.NewGuid();
            var chineseId = Guid.NewGuid();

            // Tạo các Guid cố định cho từng chương trình (Program)
            var enProgram1_Reflex = Guid.NewGuid();
            var enProgram2_Pronounce = Guid.NewGuid();
            var enProgram3_TestPrep = Guid.NewGuid();
            var enProgram4_Business = Guid.NewGuid();

            var jaProgram1_Kaiwa = Guid.NewGuid();
            var jaProgram2_Hatsuon = Guid.NewGuid();
            var jaProgram3_Business = Guid.NewGuid();

            var zhProgram1_Kouyu = Guid.NewGuid();
            var zhProgram2_Tones = Guid.NewGuid();
            var zhProgram3_HSKK = Guid.NewGuid();
            var zhProgram4_Business = Guid.NewGuid();

            var adminRoleId = Guid.NewGuid();
            var managerRoleId = Guid.NewGuid();
            var teacherRoleId = Guid.NewGuid();
            var learnerRoleId = Guid.NewGuid();

            var adminUserId = Guid.NewGuid();
            var managerEnUserId = Guid.NewGuid();
            var managerJpUserId = Guid.NewGuid();
            var managerZhUserId = Guid.NewGuid();

            var (adminHash, adminSalt) = PasswordHelper.CreatePasswordHash("Flearn@123");
            var (managerHash, managerSalt) = PasswordHelper.CreatePasswordHash("Manager@123");

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = adminRoleId, Name = "Admin", Description = "System administrator with full access", CreatedAt = now },
                new Role { RoleID = managerRoleId, Name = "Manager", Description = "Manager member for specific language support", CreatedAt = now },
                new Role { RoleID = teacherRoleId, Name = "Teacher", Description = "Teacher who can create and manage courses", CreatedAt = now },
                new Role { RoleID = learnerRoleId, Name = "Learner", Description = "Student learning languages", CreatedAt = now });

            modelBuilder.Entity<CertificateType>().HasData(
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
                new Language { LanguageID = englishId, LanguageName = "English", LanguageCode = "en", CreatedAt = now },
                new Language { LanguageID = japaneseId, LanguageName = "Japanese", LanguageCode = "ja", CreatedAt = now },
                new Language { LanguageID = chineseId, LanguageName = "Chinese", LanguageCode = "zh", CreatedAt = now }
            );

            var languageLevels = new List<LanguageLevel>
            {
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = englishId,
                    LevelName = "A1",
                    Description = "Hiểu và dùng các cụm từ/câu cơ bản cho nhu cầu giao tiếp thiết yếu.",
                    OrderIndex = 1,
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = englishId,
                    LevelName = "A2",
                    Description = "Giao tiếp tình huống đơn giản, quen thuộc.",
                    OrderIndex = 2,
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = englishId,
                    LevelName = "B1",
                    Description = "Xử lý tình huống du lịch, nói/viết về chủ đề quen thuộc.",
                    OrderIndex = 3
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = englishId,
                    LevelName = "B2",
                    Description = "Giao tiếp trôi chảy tự nhiên, hiểu văn bản chi tiết (cấp độ thường dùng cho quốc tế).",
                    OrderIndex = 4
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = englishId,
                    LevelName = "C1",
                    Description = "Sử dụng linh hoạt cho mục đích học thuật/chuyên môn, tạo văn bản phức tạp.",
                    OrderIndex = 5
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = englishId,
                    LevelName = "C2",
                    Description = "Gần như người bản xứ, hiểu mọi thứ và diễn đạt tinh tế trong mọi tình huống.",
                    OrderIndex = 6
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = japaneseId,
                    LevelName = "N5",
                    Description = "Sơ cấp thấp. Hiểu từ vựng, ngữ pháp rất cơ bản. Chỉ giao tiếp được các câu chào hỏi, giới thiệu đơn giản.",
                    OrderIndex = 1
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = japaneseId,
                    LevelName = "N4",
                    Description = "Sơ cấp. Hiểu được tiếng Nhật căn bản dùng trong tình huống thường ngày.",
                    OrderIndex = 2
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = japaneseId,
                    LevelName = "N3",
                    Description = "Trung cấp. Cầu nối giữa sơ cấp và cao cấp. Có thể tham gia vào các cuộc trò chuyện hàng ngày tự nhiên hơn.",
                    OrderIndex = 3
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = japaneseId,
                    LevelName = "N2",
                    Description = "Trung – Cao cấp. Có khả năng sử dụng tiếng Nhật trong công việc, học tập và hầu hết các tình huống.",
                    OrderIndex = 4
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = japaneseId,
                    LevelName = "N1",
                    Description = "Cao cấp. Hiểu được các tài liệu phức tạp, trừu tượng, và sử dụng ngôn ngữ chính xác, lưu loát.",
                    OrderIndex = 5
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = chineseId,
                    LevelName = "HSK 1",
                    Description = "Rất cơ bản. Hiểu và sử dụng được các cụm từ đơn giản, đáp ứng nhu cầu giao tiếp cụ thể.",
                    OrderIndex = 1
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = chineseId,
                    LevelName = "HSK 2",
                    Description = "Hiểu được các câu đơn giản về chủ đề quen thuộc và có thể trao đổi thông tin trực tiếp.",
                    OrderIndex = 2
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = chineseId,
                    LevelName = "HSK 3",
                    Description = "Có thể giao tiếp bằng tiếng Trung trong các tình huống cơ bản của cuộc sống, học tập và công việc.",
                    OrderIndex = 3
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = chineseId,
                    LevelName = "HSK 4",
                    Description = "Có thể thảo luận về nhiều chủ đề, diễn đạt ý kiến trôi chảy và hiểu văn bản phức tạp hơn.",
                    OrderIndex = 4
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = chineseId,
                    LevelName = "HSK 5",
                    Description = "Có khả năng đọc báo, xem phim, và diễn thuyết bằng tiếng Trung. Đủ cho học tập chuyên sâu.",
                    OrderIndex = 5
                },
                new LanguageLevel
                {
                    LanguageLevelID = Guid.NewGuid(),
                    LanguageID = chineseId,
                    LevelName = "HSK 6",
                    Description = "Có thể hiểu mọi thông tin nghe hoặc đọc, diễn đạt ý kiến lưu loát, chính xác và tinh tế gần như người bản xứ.",
                    OrderIndex = 6
                }
            };
            modelBuilder.Entity<LanguageLevel>().HasData(languageLevels);

            // Seed bảng Program với các Guid đã định nghĩa ở trên
            modelBuilder.Entity<Program>().HasData(
                new Program
                {
                    ProgramId = enProgram1_Reflex,
                    LanguageId = englishId,
                    Name = "Phản xạ Giao tiếp (Communication & Reflex)",
                    Description = "Lộ trình tập trung vào phản xạ nhanh, nói trôi chảy trong các tình huống đời thường.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = enProgram2_Pronounce,
                    LanguageId = englishId,
                    Name = "Luyện Phát âm & Ngữ điệu (Pronunciation & Intonation)",
                    Description = "Lộ trình sửa âm (IPA), trọng âm, nối âm và ngữ điệu tự nhiên.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = enProgram3_TestPrep,
                    LanguageId = englishId,
                    Name = "Luyện thi Nói (Speaking Test Prep)",
                    Description = "Lộ trình chiến lược, từ vựng và cấu trúc để đạt điểm cao trong các bài thi nói (IELTS, TOEIC Speaking).",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = enProgram4_Business,
                    LanguageId = englishId,
                    Name = "Thuyết trình & Thương mại (Public Speaking & Business)",
                    Description = "Lộ trình rèn luyện kỹ năng thuyết trình, họp, đàm phán trong môi trường công sở.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = jaProgram1_Kaiwa,
                    LanguageId = japaneseId,
                    Name = "Giao tiếp Hội thoại (General Kaiwa)",
                    Description = "Lộ trình luyện Kaiwa (hội thoại) trôi chảy trong các tình huống đời sống.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = jaProgram2_Hatsuon,
                    LanguageId = japaneseId,
                    Name = "Luyện Phát âm (Hatsuon)",
                    Description = "Lộ trình luyện phát âm (Hatsuon) chuẩn, đúng ngữ điệu của người Nhật.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = jaProgram3_Business,
                    LanguageId = japaneseId,
                    Name = "Hội thoại Thương mại & Kính ngữ (Business Kaiwa & Keigo)",
                    Description = "Lộ trình chuyên sâu về Kính ngữ (Keigo), giao tiếp công sở và phỏng vấn.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = zhProgram1_Kouyu,
                    LanguageId = chineseId,
                    Name = "Giao tiếp Khẩu ngữ (General Kouyu)",
                    Description = "Lộ trình luyện Khẩu ngữ (Kouyu) trôi chảy, giao tiếp hàng ngày.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = zhProgram2_Tones,
                    LanguageId = chineseId,
                    Name = "Luyện Phát âm & Thanh điệu (Pronunciation & Tones)",
                    Description = "Lộ trình luyện Pinyin và Thanh điệu (Shengdiao) chuẩn.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = zhProgram3_HSKK,
                    LanguageId = chineseId,
                    Name = "Luyện thi Nói (HSKK Preparation)",
                    Description = "Lộ trình luyện thi HSKK (thi nói) các cấp Sơ, Trung, Cao cấp.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Program
                {
                    ProgramId = zhProgram4_Business,
                    LanguageId = chineseId,
                    Name = "Hội thoại Thương mại (Business Kouyu)",
                    Description = "Lộ trình đàm phán, giao tiếp trong kinh doanh và thương mại.",
                    Status = true,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            );


            var levels = new List<Level>();

          
            var enLevels = languageLevels.Where(l => l.LanguageID == englishId);
            var enPrograms = new[] { enProgram1_Reflex, enProgram2_Pronounce, enProgram3_TestPrep, enProgram4_Business };
            foreach (var progId in enPrograms)
            {
                foreach (var langLevel in enLevels)
                {
                    levels.Add(new Level
                    {
                        LevelId = Guid.NewGuid(),
                        ProgramId = progId,
                        Name = langLevel.LevelName,
               
                        OrderIndex = langLevel.OrderIndex,
                        Description = langLevel.Description
                    });
                }
            }

      
            var jaLevels = languageLevels.Where(l => l.LanguageID == japaneseId);
            var jaPrograms = new[] { jaProgram1_Kaiwa, jaProgram2_Hatsuon, jaProgram3_Business };
            foreach (var progId in jaPrograms)
            {
                foreach (var langLevel in jaLevels)
                {
                    levels.Add(new Level
                    {
                        LevelId = Guid.NewGuid(),
                        ProgramId = progId,
                        Name = langLevel.LevelName,
                        OrderIndex = langLevel.OrderIndex,
                        Description = langLevel.Description
                    });
                }
            }

            var zhLevels = languageLevels.Where(l => l.LanguageID == chineseId);
            var zhPrograms = new[] { zhProgram1_Kouyu, zhProgram2_Tones, zhProgram3_HSKK, zhProgram4_Business };
            foreach (var progId in zhPrograms)
            {
                foreach (var langLevel in zhLevels)
                {
                    levels.Add(new Level
                    {
                        LevelId = Guid.NewGuid(),
                        ProgramId = progId,
                        Name = langLevel.LevelName,
                        OrderIndex = langLevel.OrderIndex,
                        Description = langLevel.Description
                    });
                }
            }

            modelBuilder.Entity<Level>().HasData(levels);
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserID = adminUserId,
                    UserName = "Admin",
                    Email = "admin@flearn.com",
                    PasswordHash = adminHash,
                    PasswordSalt = adminSalt,
                    IsEmailConfirmed = true,
                    DateOfBirth = new DateTime(1990, 1, 1),
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new User
                {
                    UserID = managerEnUserId,
                    UserName = "ManagerEN",
                    Email = "manager.english@flearn.com",
                    PasswordHash = managerHash,
                    PasswordSalt = managerSalt,
                    IsEmailConfirmed = true,
                    DateOfBirth = new DateTime(1992, 3, 15),
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new User
                {
                    UserID = managerJpUserId,
                    UserName = "ManagerJA",
                    Email = "manager.japanese@flearn.com",
                    PasswordHash = managerHash,
                    PasswordSalt = managerSalt,
                    IsEmailConfirmed = true,
                    DateOfBirth = new DateTime(1991, 7, 22),
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                new User
                {
                    UserID = managerZhUserId,
                    UserName = "ManagerZH",
                    Email = "manager.chinese@flearn.com",
                    PasswordHash = managerHash,
                    PasswordSalt = managerSalt,
                    IsEmailConfirmed = true,
                    DateOfBirth = new DateTime(1993, 12, 8),
                    CreatedAt = now,
                    UpdatedAt = now,
                }
            );


            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = adminUserId, RoleID = adminRoleId },
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = managerEnUserId, RoleID = managerRoleId },
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = managerJpUserId, RoleID = managerRoleId },
                new UserRole { UserRoleID = Guid.NewGuid(), UserID = managerZhUserId, RoleID = managerRoleId }
            );

            modelBuilder.Entity<ManagerLanguage>().HasData(
                new ManagerLanguage { ManagerId = Guid.NewGuid(), UserId = managerEnUserId, LanguageId = englishId, CreatedAt = now, UpdatedAt = now },
                new ManagerLanguage { ManagerId = Guid.NewGuid(), UserId = managerJpUserId, LanguageId = japaneseId, CreatedAt = now, UpdatedAt = now },
                new ManagerLanguage { ManagerId = Guid.NewGuid(), UserId = managerZhUserId, LanguageId = chineseId, CreatedAt = now, UpdatedAt = now }
            );

            modelBuilder.Entity<Topic>().HasData(
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Chào hỏi và giới thiệu bản thân",
                    Description = "Các mẫu câu và từ vựng cơ bản để chào hỏi, giới thiệu tên, tuổi, nghề nghiệp, quốc tịch.",
                    ImageUrl = "https://i.pinimg.com/1200x/b0/ca/36/b0ca36fefdde2105f25ae5884e7ff544.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Du lịch và phương tiện di chuyển",
                    Description = "Từ vựng và hội thoại về đặt vé, hỏi đường, đi taxi, sân bay và khách sạn.",
                    ImageUrl = "https://i.pinimg.com/736x/39/ea/57/39ea57e52b4e916cd923c40dd15dc066.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Mua sắm và tiền bạc",
                    Description = "Tình huống giao tiếp khi mua hàng, hỏi giá, mặc cả, và thanh toán.",
                    ImageUrl = "https://i.pinimg.com/1200x/c5/9c/ce/c59cce10929410537aa224149cf5aed0.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Ẩm thực và nhà hàng",
                    Description = "Gọi món, hỏi về món ăn, và nói chuyện với nhân viên phục vụ.",
                    ImageUrl = "https://i.pinimg.com/1200x/79/c3/7f/79c37ff715470fbd50c5f22fcdb024c6.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Công việc và phỏng vấn",
                    Description = "Giao tiếp nơi công sở, viết email, và trả lời phỏng vấn xin việc.",
                    ImageUrl = "https://i.pinimg.com/1200x/54/ad/d9/54add9aba56878ea2ddd0fe4645cf4c4.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Sở thích và thời gian rảnh",
                    Description = "Học cách nói về sở thích, thói quen, thể thao và các hoạt động giải trí.",
                    ImageUrl = "https://i.pinimg.com/736x/c6/a7/5b/c6a75b07a24c033793fb6ca2e4bd1de5.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Gia đình và bạn bè",
                    Description = "Từ vựng và câu giao tiếp nói về các mối quan hệ cá nhân.",
                    ImageUrl = "https://i.pinimg.com/1200x/39/e4/20/39e420b675af1e5e61a4b68c2d08d08c.jpg",
                    Status = true
                },
                new Topic
                {
                    TopicID = Guid.NewGuid(),
                    Name = "Trường học và học tập",
                    Description = "Các chủ đề về lớp học, bài tập, giáo viên, kỳ thi và kết quả học tập.",
                    ImageUrl = "https://i.pinimg.com/1200x/90/67/eb/9067ebc61b7b972efc65225fd6094892.jpg",
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