using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class d : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    AchievementID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_general_ci"),
                    IconUrl = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    Critertia = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.AchievementID);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    LanguageID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    LanguageName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_general_ci"),
                    LanguageCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, collation: "utf8mb4_general_ci"),
                    CreateAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.LanguageID);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    PurchasesID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    PurchasedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.PurchasesID);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "RegistrationOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    OtpCode = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false, collation: "utf8mb4_general_ci"),
                    ExpireAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationOtps", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "TempRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    UserName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_general_ci"),
                    PasswordHash = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    PasswordSalt = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    OtpCode = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false, collation: "utf8mb4_general_ci"),
                    ExpireAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempRegistrations", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    TopicID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.TopicID);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    UserAchievementID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    AchievementID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    AchievedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.UserAchievementID);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_general_ci"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    PasswordHash = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    PasswordSalt = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    LastAcessAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    JobTitle = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_general_ci"),
                    Industry = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_general_ci"),
                    StreakDays = table.Column<int>(type: "int", nullable: true),
                    Interests = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_general_ci"),
                    BirthDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MfaEnabled = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ProfilePictureUrl = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true, collation: "utf8mb4_general_ci"),
                    IsEmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    ConversationID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    LanguageID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AIFeedBackID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Topic = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.ConversationID);
                    table.ForeignKey(
                        name: "FK_Conversations_Languages_LanguageID",
                        column: x => x.LanguageID,
                        principalTable: "Languages",
                        principalColumn: "LanguageID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Conversations_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    CoverImageUrl = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false, collation: "utf8mb4_general_ci"),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    TeacherID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    LanguageID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Goal = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_general_ci"),
                    Level = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    SkillFocus = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_general_ci"),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NumLessons = table.Column<int>(type: "int", nullable: false),
                    ApprovedByID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.CourseID);
                    table.ForeignKey(
                        name: "FK_Courses_Languages_LanguageID",
                        column: x => x.LanguageID,
                        principalTable: "Languages",
                        principalColumn: "LanguageID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Courses_Users_TeacherID",
                        column: x => x.TeacherID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "LanguageUser",
                columns: table => new
                {
                    LanguagesLanguageID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UsersUserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageUser", x => new { x.LanguagesLanguageID, x.UsersUserID });
                    table.ForeignKey(
                        name: "FK_LanguageUser_Languages_LanguagesLanguageID",
                        column: x => x.LanguagesLanguageID,
                        principalTable: "Languages",
                        principalColumn: "LanguageID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LanguageUser_Users_UsersUserID",
                        column: x => x.UsersUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    RefreshTokenID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Token = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_general_ci"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsRevoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.RefreshTokenID);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Roadmaps",
                columns: table => new
                {
                    RoadmapID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    LanguageID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    CurrentLevel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    TargetLevel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    EstimatedDuration = table.Column<int>(type: "int", nullable: false),
                    DurationUnit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Progress = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roadmaps", x => x.RoadmapID);
                    table.ForeignKey(
                        name: "FK_Roadmaps_Languages_LanguageID",
                        column: x => x.LanguageID,
                        principalTable: "Languages",
                        principalColumn: "LanguageID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Roadmaps_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesRoleID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UsersUserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesRoleID, x.UsersUserID });
                    table.ForeignKey(
                        name: "FK_RoleUser_Roles_RolesRoleID",
                        column: x => x.RolesRoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleUser_Users_UsersUserID",
                        column: x => x.UsersUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "TeacherApplications",
                columns: table => new
                {
                    TeacherApplicationID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Motivation = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    AppliedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SubmitAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReviewAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    RejectionReason = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Status = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TeacherCredentialID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    LanguageID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherApplications", x => x.TeacherApplicationID);
                    table.ForeignKey(
                        name: "FK_TeacherApplications_Languages_LanguageID",
                        column: x => x.LanguageID,
                        principalTable: "Languages",
                        principalColumn: "LanguageID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherApplications_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "UserLanguages",
                columns: table => new
                {
                    UserLearningLanguageID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    LanguageID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLanguages", x => x.UserLearningLanguageID);
                    table.ForeignKey(
                        name: "FK_UserLanguages_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserRoleID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    RoleID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.UserRoleID);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "AIFeedBacks",
                columns: table => new
                {
                    AIFeedBackID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    ConversationID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Content = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    FeedbackText = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    FluencyScore = table.Column<int>(type: "int", nullable: false),
                    PronunciationScore = table.Column<int>(type: "int", nullable: false),
                    GrammarScore = table.Column<int>(type: "int", nullable: false),
                    VocabularyScore = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: true, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIFeedBacks", x => x.AIFeedBackID);
                    table.ForeignKey(
                        name: "FK_AIFeedBacks_Conversations_ConversationID",
                        column: x => x.ConversationID,
                        principalTable: "Conversations",
                        principalColumn: "ConversationID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Recordings",
                columns: table => new
                {
                    RecordingID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Url = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    LanguageID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    FilePath = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Duration = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConverationID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    ConversationID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Format = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recordings", x => x.RecordingID);
                    table.ForeignKey(
                        name: "FK_Recordings_Conversations_ConversationID",
                        column: x => x.ConversationID,
                        principalTable: "Conversations",
                        principalColumn: "ConversationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recordings_Languages_LanguageID",
                        column: x => x.LanguageID,
                        principalTable: "Languages",
                        principalColumn: "LanguageID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "CourseSubmissions",
                columns: table => new
                {
                    CourseSubmissionID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    SubmittedBy = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    SubmitterUserID = table.Column<Guid>(type: "char(36)", nullable: true, collation: "utf8mb4_general_ci"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CourseID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewBy = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    ReviewComment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSubmissions", x => x.CourseSubmissionID);
                    table.ForeignKey(
                        name: "FK_CourseSubmissions_Courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSubmissions_Users_SubmitterUserID",
                        column: x => x.SubmitterUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "CourseTopics",
                columns: table => new
                {
                    CourseTopicID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    CourseID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    TopicID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTopics", x => x.CourseTopicID);
                    table.ForeignKey(
                        name: "FK_CourseTopics_Courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseTopics_Topics_TopicID",
                        column: x => x.TopicID,
                        principalTable: "Topics",
                        principalColumn: "TopicID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "CourseUnits",
                columns: table => new
                {
                    CourseUnitID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_general_ci"),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CourseID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseUnits", x => x.CourseUnitID);
                    table.ForeignKey(
                        name: "FK_CourseUnits_Courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    EnrollmentID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    CourseID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    EnrolledAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Progress = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.EnrollmentID);
                    table.ForeignKey(
                        name: "FK_Enrollments_Courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "PurchasesDetails",
                columns: table => new
                {
                    PurchasesDetailID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    PurchasesID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    CourseID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchasesDetails", x => x.PurchasesDetailID);
                    table.ForeignKey(
                        name: "FK_PurchasesDetails_Courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchasesDetails_Purchases_PurchasesID",
                        column: x => x.PurchasesID,
                        principalTable: "Purchases",
                        principalColumn: "PurchasesID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "RoadmapDetails",
                columns: table => new
                {
                    RoadmapDetailID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    RoadmapID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    Skills = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_general_ci"),
                    Resources = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    EstimatedHours = table.Column<int>(type: "int", nullable: false),
                    DifficultyLevel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapDetails", x => x.RoadmapDetailID);
                    table.ForeignKey(
                        name: "FK_RoadmapDetails_Roadmaps_RoadmapID",
                        column: x => x.RoadmapID,
                        principalTable: "Roadmaps",
                        principalColumn: "RoadmapID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "TeacherCredentials",
                columns: table => new
                {
                    TeacherCredentialID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    CredentialName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    CredentialFileUrl = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false, collation: "utf8mb4_general_ci"),
                    ApplicationID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherCredentials", x => x.TeacherCredentialID);
                    table.ForeignKey(
                        name: "FK_TeacherCredentials_TeacherApplications_ApplicationID",
                        column: x => x.ApplicationID,
                        principalTable: "TeacherApplications",
                        principalColumn: "TeacherApplicationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherCredentials_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    LessonID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    Content = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false, collation: "utf8mb4_general_ci"),
                    Position = table.Column<int>(type: "int", nullable: false),
                    SkillFocus = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_general_ci"),
                    IsPublished = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CourseUnitID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    CreateAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.LessonID);
                    table.ForeignKey(
                        name: "FK_Lessons_CourseUnits_CourseUnitID",
                        column: x => x.CourseUnitID,
                        principalTable: "CourseUnits",
                        principalColumn: "CourseUnitID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    ExerciseID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Hints = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_general_ci"),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Materials = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    ExpectedAnswer = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    Prompt = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    LessonID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Content = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.ExerciseID);
                    table.ForeignKey(
                        name: "FK_Exercises_Lessons_LessonID",
                        column: x => x.LessonID,
                        principalTable: "Lessons",
                        principalColumn: "LessonID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    ReportedUserID = table.Column<Guid>(type: "char(36)", nullable: true, collation: "utf8mb4_general_ci"),
                    ReportedCourseID = table.Column<Guid>(type: "char(36)", nullable: true, collation: "utf8mb4_general_ci"),
                    ReportedLessonID = table.Column<Guid>(type: "char(36)", nullable: true, collation: "utf8mb4_general_ci"),
                    Reason = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    ReportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "utf8mb4_general_ci"),
                    ReviewComment = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    CreateAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Content = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, collation: "utf8mb4_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportID);
                    table.ForeignKey(
                        name: "FK_Reports_Courses_ReportedCourseID",
                        column: x => x.ReportedCourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID");
                    table.ForeignKey(
                        name: "FK_Reports_Lessons_ReportedLessonID",
                        column: x => x.ReportedLessonID,
                        principalTable: "Lessons",
                        principalColumn: "LessonID");
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReportedUserID",
                        column: x => x.ReportedUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Reports_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "AchievementID", "Critertia", "Description", "IconUrl", "Title" },
                values: new object[,]
                {
                    { new Guid("1fe0a2d9-2ab3-4d24-b363-1a8eb29e2a7b"), "7 consecutive days of learning", "Maintain a 7-day learning streak", "🔥", "Week Warrior" },
                    { new Guid("56aae191-004a-4356-b9fe-9612c9355a1f"), "Complete 1 course", "Complete your first course", "🏆", "Course Completion" },
                    { new Guid("a1370cd0-9f8d-4d53-ae8e-ba0a747d4590"), "Complete 1 lesson", "Complete your first lesson", "🎯", "First Steps" }
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "LanguageID", "CreateAt", "LanguageCode", "LanguageName" },
                values: new object[,]
                {
                    { new Guid("15fb35b9-a104-4efe-ab6a-a542bbd72ea5"), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "JP", "Japanese" },
                    { new Guid("b225740d-7c5c-4de0-a196-83d6405517db"), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "EN", "English" },
                    { new Guid("d696a74c-2e22-45e0-b81f-820bcfea2d82"), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "ZH", "Chinese" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("170397fd-92d8-4db0-bf04-f0459cba3868"), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "Teacher who can create and manage courses", "Teacher" },
                    { new Guid("26783bd9-52ba-4388-8e00-a9bcc1668022"), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "Staff member for specific language support", "Staff" },
                    { new Guid("c7957c0e-a79c-484e-9b11-a7a0c6360fa7"), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "System administrator with full access", "Admin" },
                    { new Guid("d312450c-6a8b-430a-a54e-56b32ff2c6bc"), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "Student learning languages", "Learner" }
                });

            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "TopicID", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("3744893a-9ca0-480c-88dc-17ced5e300dc"), "Pronunciation and speaking skills", "Pronunciation" },
                    { new Guid("ad6e7520-fe87-49bc-ab83-980addee0edb"), "Basic and advanced grammar concepts", "Grammar" },
                    { new Guid("e4158acd-d368-4e00-ab74-cdb432774b43"), "Practical conversation skills and dialogues", "Conversation" },
                    { new Guid("e864b8b0-fffe-44a8-ac02-6b049d2e033c"), "Essential vocabulary for daily communication", "Vocabulary" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "BirthDate", "CreatedAt", "Email", "Industry", "Interests", "IsEmailConfirmed", "JobTitle", "LastAcessAt", "MfaEnabled", "PasswordHash", "PasswordSalt", "ProfilePictureUrl", "Status", "StreakDays", "UpdateAt", "UserName" },
                values: new object[,]
                {
                    { new Guid("17b560b9-d332-4aea-b8a1-7a90f28b3055"), new DateTime(1992, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "staff.english@flearn.com", "Education", "English Language Support", true, "English Language Staff", new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), false, "b4SV/UcO4AGyONnIaTSLsCieORDEwpfIr290AgL73+8hLm2vEOqeu2IhVTKG3ASswTTaFUopnlNe9YwnVUrUqQ==", "V3GDTSWM6caqKKS7YmtyqXVXaoznHplo+a5FMZC39qvjPe0ux+HBejSqDrlJqkk3iITesw1XoftYf4dd1nSV65qeNYDUU9mYM+0Ynf7IXy+rrbX5W5sNd+d1FkYvIsLP2ZWvivc9elS1ccbinV8LpVNnlJm4x76c0zTIhViDzbI=", "", true, 0, new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "StaffEN" },
                    { new Guid("72757900-03f9-4bf6-9f52-5a030466bbf3"), new DateTime(1991, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "staff.japanese@flearn.com", "Education", "Japanese Language Support", true, "Japanese Language Staff", new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), false, "b4SV/UcO4AGyONnIaTSLsCieORDEwpfIr290AgL73+8hLm2vEOqeu2IhVTKG3ASswTTaFUopnlNe9YwnVUrUqQ==", "V3GDTSWM6caqKKS7YmtyqXVXaoznHplo+a5FMZC39qvjPe0ux+HBejSqDrlJqkk3iITesw1XoftYf4dd1nSV65qeNYDUU9mYM+0Ynf7IXy+rrbX5W5sNd+d1FkYvIsLP2ZWvivc9elS1ccbinV8LpVNnlJm4x76c0zTIhViDzbI=", "", true, 0, new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "StaffJP" },
                    { new Guid("a8ecb768-a4ec-409d-9703-d8feae50fb1f"), new DateTime(1993, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "staff.chinese@flearn.com", "Education", "Chinese Language Support", true, "Chinese Language Staff", new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), false, "b4SV/UcO4AGyONnIaTSLsCieORDEwpfIr290AgL73+8hLm2vEOqeu2IhVTKG3ASswTTaFUopnlNe9YwnVUrUqQ==", "V3GDTSWM6caqKKS7YmtyqXVXaoznHplo+a5FMZC39qvjPe0ux+HBejSqDrlJqkk3iITesw1XoftYf4dd1nSV65qeNYDUU9mYM+0Ynf7IXy+rrbX5W5sNd+d1FkYvIsLP2ZWvivc9elS1ccbinV8LpVNnlJm4x76c0zTIhViDzbI=", "", true, 0, new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "StaffZH" },
                    { new Guid("dce0b328-2ccc-43b5-9e6b-78a6c36a6ca4"), new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "admin@flearn.com", "Education Technology", "System Management", true, "System Administrator", new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), false, "5YReGHJnh1atvSpK9Izx5JO3q9VwI5IEFhaSkez+8r2cnu21O0z8lFi1qHH7WUK2MmuvpyhdzYqBvgFmbcvXKg==", "FLykKtzaRC3Xp7+UbQBghnIbXXxoPlarDE5yEUqif67kjBI7GYF8aOvCZ1V59g1JHRIqd88UwqCL48jQCpFhauEWQ/ET9E5tq1sBF7Yn6UThw6PL69/6vI+nP86W3i2T+SLRVo+e23dmmVBGbL+onmE1B/p1DvXy9Wpnw2mUahE=", "", true, 0, new DateTime(2025, 9, 18, 6, 39, 46, 524, DateTimeKind.Utc).AddTicks(655), "Flearn" }
                });

            migrationBuilder.InsertData(
                table: "UserLanguages",
                columns: new[] { "UserLearningLanguageID", "LanguageID", "UserID" },
                values: new object[,]
                {
                    { new Guid("5d02ccf1-7238-4ebd-b773-e20bc7af04da"), new Guid("d696a74c-2e22-45e0-b81f-820bcfea2d82"), new Guid("a8ecb768-a4ec-409d-9703-d8feae50fb1f") },
                    { new Guid("7f660073-5648-499d-88df-40ba83e174f8"), new Guid("15fb35b9-a104-4efe-ab6a-a542bbd72ea5"), new Guid("72757900-03f9-4bf6-9f52-5a030466bbf3") },
                    { new Guid("a7524860-cd8f-4913-934b-f6c8dedeb8ae"), new Guid("b225740d-7c5c-4de0-a196-83d6405517db"), new Guid("17b560b9-d332-4aea-b8a1-7a90f28b3055") }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "UserRoleID", "RoleID", "UserID" },
                values: new object[,]
                {
                    { new Guid("717420f5-d273-41e6-89f7-373e966326de"), new Guid("26783bd9-52ba-4388-8e00-a9bcc1668022"), new Guid("72757900-03f9-4bf6-9f52-5a030466bbf3") },
                    { new Guid("7a4cd38b-ccc4-4e69-b921-6a0a81264209"), new Guid("26783bd9-52ba-4388-8e00-a9bcc1668022"), new Guid("17b560b9-d332-4aea-b8a1-7a90f28b3055") },
                    { new Guid("ce98e78e-cafa-4ed3-9941-df440dd425d8"), new Guid("26783bd9-52ba-4388-8e00-a9bcc1668022"), new Guid("a8ecb768-a4ec-409d-9703-d8feae50fb1f") },
                    { new Guid("cf38dfc8-bd04-44a9-80c4-ae790f48c717"), new Guid("c7957c0e-a79c-484e-9b11-a7a0c6360fa7"), new Guid("dce0b328-2ccc-43b5-9e6b-78a6c36a6ca4") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIFeedBacks_ConversationID",
                table: "AIFeedBacks",
                column: "ConversationID");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LanguageID",
                table: "Conversations",
                column: "LanguageID");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserID",
                table: "Conversations",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_LanguageID",
                table: "Courses",
                column: "LanguageID");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TeacherID",
                table: "Courses",
                column: "TeacherID");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSubmissions_CourseID",
                table: "CourseSubmissions",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSubmissions_SubmitterUserID",
                table: "CourseSubmissions",
                column: "SubmitterUserID");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTopics_CourseID",
                table: "CourseTopics",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTopics_TopicID",
                table: "CourseTopics",
                column: "TopicID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseUnits_CourseID",
                table: "CourseUnits",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseID",
                table: "Enrollments",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_LessonID",
                table: "Exercises",
                column: "LessonID");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageUser_UsersUserID",
                table: "LanguageUser",
                column: "UsersUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseUnitID",
                table: "Lessons",
                column: "CourseUnitID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasesDetails_CourseID",
                table: "PurchasesDetails",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasesDetails_PurchasesID",
                table: "PurchasesDetails",
                column: "PurchasesID");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_ConversationID",
                table: "Recordings",
                column: "ConversationID");

            migrationBuilder.CreateIndex(
                name: "IX_Recordings_LanguageID",
                table: "Recordings",
                column: "LanguageID");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserID",
                table: "RefreshTokens",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedCourseID",
                table: "Reports",
                column: "ReportedCourseID");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedLessonID",
                table: "Reports",
                column: "ReportedLessonID");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedUserID",
                table: "Reports",
                column: "ReportedUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UserID",
                table: "Reports",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapDetails_RoadmapID_StepNumber",
                table: "RoadmapDetails",
                columns: new[] { "RoadmapID", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roadmaps_LanguageID",
                table: "Roadmaps",
                column: "LanguageID");

            migrationBuilder.CreateIndex(
                name: "IX_Roadmaps_UserID",
                table: "Roadmaps",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UsersUserID",
                table: "RoleUser",
                column: "UsersUserID");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherApplications_LanguageID",
                table: "TeacherApplications",
                column: "LanguageID");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherApplications_UserID",
                table: "TeacherApplications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherCredentials_ApplicationID",
                table: "TeacherCredentials",
                column: "ApplicationID");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherCredentials_UserID",
                table: "TeacherCredentials",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserLanguages_UserID",
                table: "UserLanguages",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleID",
                table: "UserRoles",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserID",
                table: "UserRoles",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "AIFeedBacks");

            migrationBuilder.DropTable(
                name: "CourseSubmissions");

            migrationBuilder.DropTable(
                name: "CourseTopics");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "LanguageUser");

            migrationBuilder.DropTable(
                name: "PurchasesDetails");

            migrationBuilder.DropTable(
                name: "Recordings");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RegistrationOtps");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "RoadmapDetails");

            migrationBuilder.DropTable(
                name: "RoleUser");

            migrationBuilder.DropTable(
                name: "TeacherCredentials");

            migrationBuilder.DropTable(
                name: "TempRegistrations");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "UserLanguages");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Roadmaps");

            migrationBuilder.DropTable(
                name: "TeacherApplications");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "CourseUnits");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
