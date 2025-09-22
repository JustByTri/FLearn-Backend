using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetOtpAndRememberMe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("1fe0a2d9-2ab3-4d24-b363-1a8eb29e2a7b"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("56aae191-004a-4356-b9fe-9612c9355a1f"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("a1370cd0-9f8d-4d53-ae8e-ba0a747d4590"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("15fb35b9-a104-4efe-ab6a-a542bbd72ea5"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("b225740d-7c5c-4de0-a196-83d6405517db"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("d696a74c-2e22-45e0-b81f-820bcfea2d82"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("170397fd-92d8-4db0-bf04-f0459cba3868"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("d312450c-6a8b-430a-a54e-56b32ff2c6bc"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("3744893a-9ca0-480c-88dc-17ced5e300dc"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("ad6e7520-fe87-49bc-ab83-980addee0edb"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("e4158acd-d368-4e00-ab74-cdb432774b43"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("e864b8b0-fffe-44a8-ac02-6b049d2e033c"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("5d02ccf1-7238-4ebd-b773-e20bc7af04da"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("7f660073-5648-499d-88df-40ba83e174f8"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("a7524860-cd8f-4913-934b-f6c8dedeb8ae"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("717420f5-d273-41e6-89f7-373e966326de"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("7a4cd38b-ccc4-4e69-b921-6a0a81264209"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("ce98e78e-cafa-4ed3-9941-df440dd425d8"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("cf38dfc8-bd04-44a9-80c4-ae790f48c717"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("26783bd9-52ba-4388-8e00-a9bcc1668022"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("c7957c0e-a79c-484e-9b11-a7a0c6360fa7"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("17b560b9-d332-4aea-b8a1-7a90f28b3055"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("72757900-03f9-4bf6-9f52-5a030466bbf3"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("a8ecb768-a4ec-409d-9703-d8feae50fb1f"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("dce0b328-2ccc-43b5-9e6b-78a6c36a6ca4"));

            migrationBuilder.CreateTable(
                name: "PasswordResetOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8mb4_general_ci"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    OtpCode = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false, collation: "utf8mb4_general_ci"),
                    ExpireAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetOtps", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "AchievementID", "Critertia", "Description", "IconUrl", "Title" },
                values: new object[,]
                {
                    { new Guid("0b8c294e-092c-48df-9332-151585aec1c3"), "Complete 1 course", "Complete your first course", "🏆", "Course Completion" },
                    { new Guid("a2920c8c-9ffa-44dc-8c45-adad4dd0ad8e"), "Complete 1 lesson", "Complete your first lesson", "🎯", "First Steps" },
                    { new Guid("bfa9d387-1bcb-49d2-bf1c-23b53fbada31"), "7 consecutive days of learning", "Maintain a 7-day learning streak", "🔥", "Week Warrior" }
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "LanguageID", "CreateAt", "LanguageCode", "LanguageName" },
                values: new object[,]
                {
                    { new Guid("14690634-d3f0-415a-a61f-aed29fadd649"), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "ZH", "Chinese" },
                    { new Guid("1cb5775d-7f06-406a-a5aa-d40f5c1f3ac6"), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "EN", "English" },
                    { new Guid("d96e0ca4-fd59-41a2-a022-51e7273b6c41"), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "JP", "Japanese" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("13b536b5-9b46-4c55-adcd-77abf2b8148d"), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "Teacher who can create and manage courses", "Teacher" },
                    { new Guid("473498ed-7d77-4d71-afb6-28784a30172d"), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "Staff member for specific language support", "Staff" },
                    { new Guid("48a1e435-a8ff-4127-92a5-f09ffac5664d"), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "System administrator with full access", "Admin" },
                    { new Guid("8fc126fd-f7c2-4990-a1b1-7a14ddf13713"), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "Student learning languages", "Learner" }
                });

            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "TopicID", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("0529f9ec-1e14-4cc5-9661-cc9c59f50a27"), "Basic and advanced grammar concepts", "Grammar" },
                    { new Guid("094133c0-64e8-4f42-b3c3-93fc621e8317"), "Essential vocabulary for daily communication", "Vocabulary" },
                    { new Guid("8bc63455-79c7-4045-b3d6-a661de72ac92"), "Pronunciation and speaking skills", "Pronunciation" },
                    { new Guid("feee41bd-e400-4c8e-9d86-5ca5b82ad97e"), "Practical conversation skills and dialogues", "Conversation" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "BirthDate", "CreatedAt", "Email", "Industry", "Interests", "IsEmailConfirmed", "JobTitle", "LastAcessAt", "MfaEnabled", "PasswordHash", "PasswordSalt", "ProfilePictureUrl", "Status", "StreakDays", "UpdateAt", "UserName" },
                values: new object[,]
                {
                    { new Guid("4435e757-6569-4a0b-a99d-9fc7c699d498"), new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "admin@flearn.com", "Education Technology", "System Management", true, "System Administrator", new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), false, "ihVcSj+OdBMjRhQdNkX9+CVK9/UGKHmNXgBCzN29Y8U8aAcwX4QVa4NpmxBB+leKn7C12bErwed8qGe4NczgWw==", "b72h5gMt0G6Wbr1nLIshjN76+qi6cjaheq1+Ra3uqsV1J0D2Q2ekd7uP+L3PDCstIaYClElUZ45x+f5/y0P2QYE394LoeXZfJtN8BO61VfwhnzzCVGmFk1rQciERFG/O12wzDpJcDY/Z4o7AS/t7X9FXli7fKXTF+bqdShmS8ds=", "", true, 0, new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "Flearn" },
                    { new Guid("6734a39f-d663-4611-9a7f-f18e7fbac173"), new DateTime(1993, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "staff.chinese@flearn.com", "Education", "Chinese Language Support", true, "Chinese Language Staff", new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), false, "vfvSfLUTBdlN+Tn+j7KAlf7Zm3xv1o0M073JqeDvGigT5SPjGEuPJCX32Rgi2C0GFLhkMlT/U/QpXZRieQnJvw==", "InaRHN2Fy5XsaAX9rnJO4IOsdArPUeVgDWmDRGMPNRIe3xMxtyRaeORhDn9s21+sbi5Y09nGdGZJuSKLFxL6MOpvPGnG5xlfc0qmwfOfCFUGt25AqVPpeo4XCxQz2VRpDpKTRfhEcwsXbPlZ9GXcljxc8e4Na4f56z94iU0LtpY=", "", true, 0, new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "StaffZH" },
                    { new Guid("6b8d79bb-9237-4fd5-a124-cec5dc1c8ee2"), new DateTime(1992, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "staff.english@flearn.com", "Education", "English Language Support", true, "English Language Staff", new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), false, "vfvSfLUTBdlN+Tn+j7KAlf7Zm3xv1o0M073JqeDvGigT5SPjGEuPJCX32Rgi2C0GFLhkMlT/U/QpXZRieQnJvw==", "InaRHN2Fy5XsaAX9rnJO4IOsdArPUeVgDWmDRGMPNRIe3xMxtyRaeORhDn9s21+sbi5Y09nGdGZJuSKLFxL6MOpvPGnG5xlfc0qmwfOfCFUGt25AqVPpeo4XCxQz2VRpDpKTRfhEcwsXbPlZ9GXcljxc8e4Na4f56z94iU0LtpY=", "", true, 0, new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "StaffEN" },
                    { new Guid("83403895-ef2a-4795-8250-a331115cf7ce"), new DateTime(1991, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "staff.japanese@flearn.com", "Education", "Japanese Language Support", true, "Japanese Language Staff", new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), false, "vfvSfLUTBdlN+Tn+j7KAlf7Zm3xv1o0M073JqeDvGigT5SPjGEuPJCX32Rgi2C0GFLhkMlT/U/QpXZRieQnJvw==", "InaRHN2Fy5XsaAX9rnJO4IOsdArPUeVgDWmDRGMPNRIe3xMxtyRaeORhDn9s21+sbi5Y09nGdGZJuSKLFxL6MOpvPGnG5xlfc0qmwfOfCFUGt25AqVPpeo4XCxQz2VRpDpKTRfhEcwsXbPlZ9GXcljxc8e4Na4f56z94iU0LtpY=", "", true, 0, new DateTime(2025, 9, 19, 7, 8, 12, 930, DateTimeKind.Utc).AddTicks(9392), "StaffJP" }
                });

            migrationBuilder.InsertData(
                table: "UserLanguages",
                columns: new[] { "UserLearningLanguageID", "LanguageID", "UserID" },
                values: new object[,]
                {
                    { new Guid("325879bd-76c2-40ca-b79f-ff6bc9a32309"), new Guid("d96e0ca4-fd59-41a2-a022-51e7273b6c41"), new Guid("83403895-ef2a-4795-8250-a331115cf7ce") },
                    { new Guid("7bd780f3-4eef-4fcf-b45c-32a42618867e"), new Guid("14690634-d3f0-415a-a61f-aed29fadd649"), new Guid("6734a39f-d663-4611-9a7f-f18e7fbac173") },
                    { new Guid("9275d580-e726-4c33-bddc-7646f02b4405"), new Guid("1cb5775d-7f06-406a-a5aa-d40f5c1f3ac6"), new Guid("6b8d79bb-9237-4fd5-a124-cec5dc1c8ee2") }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "UserRoleID", "RoleID", "UserID" },
                values: new object[,]
                {
                    { new Guid("1997425b-9c8d-4d09-8024-8357afb5317e"), new Guid("473498ed-7d77-4d71-afb6-28784a30172d"), new Guid("83403895-ef2a-4795-8250-a331115cf7ce") },
                    { new Guid("b32d874f-64f5-4765-960e-e24fedeb2974"), new Guid("473498ed-7d77-4d71-afb6-28784a30172d"), new Guid("6734a39f-d663-4611-9a7f-f18e7fbac173") },
                    { new Guid("b574c41d-8564-4c3a-bf3a-507e2452ae00"), new Guid("473498ed-7d77-4d71-afb6-28784a30172d"), new Guid("6b8d79bb-9237-4fd5-a124-cec5dc1c8ee2") },
                    { new Guid("f1fca37d-ab36-435e-a68e-7b903eb9ec4d"), new Guid("48a1e435-a8ff-4127-92a5-f09ffac5664d"), new Guid("4435e757-6569-4a0b-a99d-9fc7c699d498") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetOtps");

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("0b8c294e-092c-48df-9332-151585aec1c3"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("a2920c8c-9ffa-44dc-8c45-adad4dd0ad8e"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("bfa9d387-1bcb-49d2-bf1c-23b53fbada31"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("14690634-d3f0-415a-a61f-aed29fadd649"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("1cb5775d-7f06-406a-a5aa-d40f5c1f3ac6"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("d96e0ca4-fd59-41a2-a022-51e7273b6c41"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("13b536b5-9b46-4c55-adcd-77abf2b8148d"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("8fc126fd-f7c2-4990-a1b1-7a14ddf13713"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("0529f9ec-1e14-4cc5-9661-cc9c59f50a27"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("094133c0-64e8-4f42-b3c3-93fc621e8317"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("8bc63455-79c7-4045-b3d6-a661de72ac92"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("feee41bd-e400-4c8e-9d86-5ca5b82ad97e"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("325879bd-76c2-40ca-b79f-ff6bc9a32309"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("7bd780f3-4eef-4fcf-b45c-32a42618867e"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("9275d580-e726-4c33-bddc-7646f02b4405"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("1997425b-9c8d-4d09-8024-8357afb5317e"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("b32d874f-64f5-4765-960e-e24fedeb2974"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("b574c41d-8564-4c3a-bf3a-507e2452ae00"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("f1fca37d-ab36-435e-a68e-7b903eb9ec4d"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("473498ed-7d77-4d71-afb6-28784a30172d"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("48a1e435-a8ff-4127-92a5-f09ffac5664d"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("4435e757-6569-4a0b-a99d-9fc7c699d498"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("6734a39f-d663-4611-9a7f-f18e7fbac173"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("6b8d79bb-9237-4fd5-a124-cec5dc1c8ee2"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("83403895-ef2a-4795-8250-a331115cf7ce"));

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
        }
    }
}
