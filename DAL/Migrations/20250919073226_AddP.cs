using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateAt",
                table: "RegistrationOtps",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "PasswordResetOtps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "PasswordResetOtps",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "PasswordResetOtps",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "PasswordResetOtps",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "AchievementID", "Critertia", "Description", "IconUrl", "Title" },
                values: new object[,]
                {
                    { new Guid("2d042fe3-5a93-4150-b068-c60de8f6392d"), "Complete 1 course", "Complete your first course", "🏆", "Course Completion" },
                    { new Guid("8d7e1be2-5673-485a-a0b5-94f7b58074b4"), "7 consecutive days of learning", "Maintain a 7-day learning streak", "🔥", "Week Warrior" },
                    { new Guid("d0266909-b317-4db7-9949-2d7828310ad5"), "Complete 1 lesson", "Complete your first lesson", "🎯", "First Steps" }
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "LanguageID", "CreateAt", "LanguageCode", "LanguageName" },
                values: new object[,]
                {
                    { new Guid("06fd8b08-f3a4-4bb3-aca3-560922bfaaa2"), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "ZH", "Chinese" },
                    { new Guid("56415da8-45fe-4e4e-b4f8-c15b0e88d50a"), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "EN", "English" },
                    { new Guid("5ba48218-82ed-4862-a66e-9a905eecc70a"), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "JP", "Japanese" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("4a2aec3b-153f-44ac-a0ab-09c94879b2c2"), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "Teacher who can create and manage courses", "Teacher" },
                    { new Guid("4b2b6076-d45c-45b3-b40d-4cb2d7f96d24"), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "Staff member for specific language support", "Staff" },
                    { new Guid("72e92ed7-48be-4b20-941d-743bc331cb2f"), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "Student learning languages", "Learner" },
                    { new Guid("fb124d92-693a-4e87-9da3-411208fe2d31"), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "System administrator with full access", "Admin" }
                });

            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "TopicID", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("18e66120-f13a-4ead-a148-2db3cf27eda0"), "Essential vocabulary for daily communication", "Vocabulary" },
                    { new Guid("280b3967-392c-463f-8909-eae0458c2860"), "Basic and advanced grammar concepts", "Grammar" },
                    { new Guid("39c21d81-9e0f-47cd-a8e0-1240cd912d25"), "Pronunciation and speaking skills", "Pronunciation" },
                    { new Guid("6c23f855-9835-4182-adee-58c0298a8d4e"), "Practical conversation skills and dialogues", "Conversation" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "BirthDate", "CreatedAt", "Email", "Industry", "Interests", "IsEmailConfirmed", "JobTitle", "LastAcessAt", "MfaEnabled", "PasswordHash", "PasswordSalt", "ProfilePictureUrl", "Status", "StreakDays", "UpdateAt", "UserName" },
                values: new object[,]
                {
                    { new Guid("1140b2d6-7d0a-4149-a609-540be03b9899"), new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "admin@flearn.com", "Education Technology", "System Management", true, "System Administrator", new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), false, "yCq9MQPLqfexxwx0HWFIfdkr/ODshzrcQ7AMHCGmXnWJSRJ1Pn8/8RYQbb7nHTRzKRoulK4fJ3t1NvioCxEZFw==", "MRcGqkaXKB7MQMnqkp9/hz++Q7xz1whMq6KdCpkqbCPr/rkQMoejqov1PjeqTsGYQ4aKO/N4tYiKKqL02m/bKOyJ4gXFjUwq0EG9tfDoC2rs695S3aRa1/JqzVnOZAyEsw3yCVIANWTXyZJVYaPHTfyvRFf2C6b3UYNortOJ0Z4=", "", true, 0, new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "Flearn" },
                    { new Guid("16d9c51b-6897-416d-bfd6-942a06d1fa49"), new DateTime(1991, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "staff.japanese@flearn.com", "Education", "Japanese Language Support", true, "Japanese Language Staff", new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), false, "1J6KA3iPEr4grMxC+P+L+W0jqz3HNhnjlrW1x5knZYvESQfQOmDzvuwmHe/TeMPUzt5/hQOm86gMLSSGzVQqJg==", "6NSdP/uxwnJ5u/GOGk8sWYemRfa9wwNoa7PvP/y3PvPY/4/ye9dGy+/FJklbVNGLOCWhqUmb0GCCstzB9RfzZb6H7ESAClsMFQF5BZNMbgab9F1KLmaRdrKOGLzSO0V1zytvVBBTb/AgWpKUvUoT4RD+RZNaRWN41ak+97RF3dY=", "", true, 0, new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "StaffJP" },
                    { new Guid("3f828c4d-4552-42cd-9aab-098c9d9f210f"), new DateTime(1992, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "staff.english@flearn.com", "Education", "English Language Support", true, "English Language Staff", new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), false, "1J6KA3iPEr4grMxC+P+L+W0jqz3HNhnjlrW1x5knZYvESQfQOmDzvuwmHe/TeMPUzt5/hQOm86gMLSSGzVQqJg==", "6NSdP/uxwnJ5u/GOGk8sWYemRfa9wwNoa7PvP/y3PvPY/4/ye9dGy+/FJklbVNGLOCWhqUmb0GCCstzB9RfzZb6H7ESAClsMFQF5BZNMbgab9F1KLmaRdrKOGLzSO0V1zytvVBBTb/AgWpKUvUoT4RD+RZNaRWN41ak+97RF3dY=", "", true, 0, new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "StaffEN" },
                    { new Guid("974d92aa-0a70-4781-895a-79c2fb78c811"), new DateTime(1993, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "staff.chinese@flearn.com", "Education", "Chinese Language Support", true, "Chinese Language Staff", new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), false, "1J6KA3iPEr4grMxC+P+L+W0jqz3HNhnjlrW1x5knZYvESQfQOmDzvuwmHe/TeMPUzt5/hQOm86gMLSSGzVQqJg==", "6NSdP/uxwnJ5u/GOGk8sWYemRfa9wwNoa7PvP/y3PvPY/4/ye9dGy+/FJklbVNGLOCWhqUmb0GCCstzB9RfzZb6H7ESAClsMFQF5BZNMbgab9F1KLmaRdrKOGLzSO0V1zytvVBBTb/AgWpKUvUoT4RD+RZNaRWN41ak+97RF3dY=", "", true, 0, new DateTime(2025, 9, 19, 7, 32, 25, 740, DateTimeKind.Utc).AddTicks(3611), "StaffZH" }
                });

            migrationBuilder.InsertData(
                table: "UserLanguages",
                columns: new[] { "UserLearningLanguageID", "LanguageID", "UserID" },
                values: new object[,]
                {
                    { new Guid("33dfe8e8-be86-443e-a015-7fb60b7b7c64"), new Guid("5ba48218-82ed-4862-a66e-9a905eecc70a"), new Guid("16d9c51b-6897-416d-bfd6-942a06d1fa49") },
                    { new Guid("70eda433-a8a1-4de2-896c-d2ac85120c88"), new Guid("06fd8b08-f3a4-4bb3-aca3-560922bfaaa2"), new Guid("974d92aa-0a70-4781-895a-79c2fb78c811") },
                    { new Guid("a3260c27-6779-4c22-be87-8cf47fbe8a70"), new Guid("56415da8-45fe-4e4e-b4f8-c15b0e88d50a"), new Guid("3f828c4d-4552-42cd-9aab-098c9d9f210f") }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "UserRoleID", "RoleID", "UserID" },
                values: new object[,]
                {
                    { new Guid("0786a6e0-9e7e-4bc4-928a-ce54e98a5edf"), new Guid("fb124d92-693a-4e87-9da3-411208fe2d31"), new Guid("1140b2d6-7d0a-4149-a609-540be03b9899") },
                    { new Guid("54eb3b22-dee5-4527-b85a-2174ef97dd94"), new Guid("4b2b6076-d45c-45b3-b40d-4cb2d7f96d24"), new Guid("16d9c51b-6897-416d-bfd6-942a06d1fa49") },
                    { new Guid("94b08a12-044d-41ef-8496-6791742133a6"), new Guid("4b2b6076-d45c-45b3-b40d-4cb2d7f96d24"), new Guid("974d92aa-0a70-4781-895a-79c2fb78c811") },
                    { new Guid("b4662c6a-aff0-4f60-b378-8cf787d0ed56"), new Guid("4b2b6076-d45c-45b3-b40d-4cb2d7f96d24"), new Guid("3f828c4d-4552-42cd-9aab-098c9d9f210f") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("2d042fe3-5a93-4150-b068-c60de8f6392d"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("8d7e1be2-5673-485a-a0b5-94f7b58074b4"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("d0266909-b317-4db7-9949-2d7828310ad5"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("06fd8b08-f3a4-4bb3-aca3-560922bfaaa2"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("56415da8-45fe-4e4e-b4f8-c15b0e88d50a"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("5ba48218-82ed-4862-a66e-9a905eecc70a"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("4a2aec3b-153f-44ac-a0ab-09c94879b2c2"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("72e92ed7-48be-4b20-941d-743bc331cb2f"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("18e66120-f13a-4ead-a148-2db3cf27eda0"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("280b3967-392c-463f-8909-eae0458c2860"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("39c21d81-9e0f-47cd-a8e0-1240cd912d25"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("6c23f855-9835-4182-adee-58c0298a8d4e"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("33dfe8e8-be86-443e-a015-7fb60b7b7c64"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("70eda433-a8a1-4de2-896c-d2ac85120c88"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("a3260c27-6779-4c22-be87-8cf47fbe8a70"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("0786a6e0-9e7e-4bc4-928a-ce54e98a5edf"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("54eb3b22-dee5-4527-b85a-2174ef97dd94"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("94b08a12-044d-41ef-8496-6791742133a6"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("b4662c6a-aff0-4f60-b378-8cf787d0ed56"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("4b2b6076-d45c-45b3-b40d-4cb2d7f96d24"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("fb124d92-693a-4e87-9da3-411208fe2d31"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("1140b2d6-7d0a-4149-a609-540be03b9899"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("16d9c51b-6897-416d-bfd6-942a06d1fa49"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("3f828c4d-4552-42cd-9aab-098c9d9f210f"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("974d92aa-0a70-4781-895a-79c2fb78c811"));

            migrationBuilder.DropColumn(
                name: "CreateAt",
                table: "RegistrationOtps");

            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "PasswordResetOtps");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "PasswordResetOtps");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "PasswordResetOtps");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "PasswordResetOtps");

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
    }
}
