using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class Init02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("23b7dcc1-329e-43ee-ad9e-e968fe241aa6"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("e0d226bc-e3d7-40cf-9f45-44203de634a1"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("fe49651c-37e7-4382-bc47-2de2c25c816d"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("2c5bd7be-5ea2-4c7b-b60b-c6c9300eadfb"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("7165b3a2-08db-4cb4-bd56-576b39e91306"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("e6b5e7d4-661f-43f1-8bd0-006d2553b94f"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("61e85b5f-99be-4501-9b8d-1394211faeec"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("fdb64d34-dd07-4220-88ac-e95d73fa4d66"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("43550bfb-9f88-44a8-9e67-0076527a9d4e"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("8b858937-3f09-4044-ac7a-28e0292aad1d"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("9c8fed74-95dc-452f-b5eb-7f3be4d67555"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("d1ba7c2c-e9e8-4545-9c2e-effa21390e21"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("50dd59db-85d8-495d-8970-81420811a5ec"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("e401f49e-8ce5-4654-90af-71cd06852f9e"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("f1ab46cd-0c8a-4724-9f83-169cdf50a462"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("25573809-51f6-45b5-9421-2f88a4426caf"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("28aa53f6-b3ff-4e37-9d5a-524f069a0141"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("c4017a37-6c51-4d41-a09c-27c4ba27eab6"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("fc087ce1-f6d3-4963-84d0-2fcce6bcf9f3"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("19e4f171-3d4e-4d9f-a811-bd0bd0e08cdb"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("d310ae94-23f1-487d-a62d-cf6b5b7a1d91"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("059b4949-2d15-4509-b05f-2b9e3162a5d7"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("86668e44-9695-4f45-b053-5bf3b35e67f9"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("94cac57f-f19f-4726-b4e7-ccb075984987"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("dac2f21d-5f96-4e3e-b720-22d0d810a19d"));

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Topics",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "AchievementID", "Critertia", "Description", "IconUrl", "Title" },
                values: new object[,]
                {
                    { new Guid("08c70a59-16b2-488a-8a92-16b7e6082ab4"), "Complete 1 course", "Complete your first course", "🏆", "Course Completion" },
                    { new Guid("551cee5e-59dc-404b-a916-1c5ab6bdcb0e"), "7 consecutive days of learning", "Maintain a 7-day learning streak", "🔥", "Week Warrior" },
                    { new Guid("cf676fa1-18e3-413d-a4a0-c337666a9d4e"), "Complete 1 lesson", "Complete your first lesson", "🎯", "First Steps" }
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "LanguageID", "CreateAt", "LanguageCode", "LanguageName" },
                values: new object[,]
                {
                    { new Guid("8bfafd1d-dd69-417c-b0a3-ba7cecf6ed52"), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "JP", "Japanese" },
                    { new Guid("b67234c2-8d16-42d4-b700-9e43f78ad841"), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "ZH", "Chinese" },
                    { new Guid("e1e28714-3c7c-445c-aef4-00b9e2f2155d"), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "EN", "English" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("1b49d836-11f2-4ce6-8d6e-aa0571a226ce"), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "Student learning languages", "Learner" },
                    { new Guid("1b5ab4fb-f440-4f90-8a52-de444bddf2cb"), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "System administrator with full access", "Admin" },
                    { new Guid("9b9ce2a4-be37-4bc2-820c-4beffa649122"), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "Teacher who can create and manage courses", "Teacher" },
                    { new Guid("dc286a49-f003-424b-a21d-e654ebcfe2ee"), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "Staff member for specific language support", "Staff" }
                });

            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "TopicID", "Description", "ImageUrl", "Name", "PublicId" },
                values: new object[,]
                {
                    { new Guid("58fcd562-594d-4a1b-8da3-7b27e7f8c115"), "Essential vocabulary for daily communication", null, "Vocabulary", null },
                    { new Guid("849a2b06-8116-4031-8f80-ee1451538cb6"), "Basic and advanced grammar concepts", null, "Grammar", null },
                    { new Guid("c93a32fd-6cad-486a-bba8-e8825d2b04d9"), "Pronunciation and speaking skills", null, "Pronunciation", null },
                    { new Guid("cc4c4592-8eeb-4e78-bf96-9f921b00bf33"), "Practical conversation skills and dialogues", null, "Conversation", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "BirthDate", "CreatedAt", "Email", "Industry", "Interests", "IsEmailConfirmed", "JobTitle", "LastAcessAt", "MfaEnabled", "PasswordHash", "PasswordSalt", "ProfilePictureUrl", "Status", "StreakDays", "UpdateAt", "UserName" },
                values: new object[,]
                {
                    { new Guid("08324f9a-3ad0-4524-9c60-9021e852ba30"), new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "admin@flearn.com", "Education Technology", "System Management", true, "System Administrator", new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), false, "XNw7ql1+yVbK6xufZ2T2gEcWBRNa86pbCPdypWW5OYhxmNmC3BMn1XXpORcvOiqh/CJG0SUMBUgUq9Tbs/3xag==", "Qx3935ia91ae7HdIzxp4VnETMRSH2uVEClhjS889IWEr9YVKHKcWBnLs0tCxz8NPGDZQ2N+Ai+8x32Q2suYc023BhPj1CaZhDmkZdZ4lpLVkPD9htgLa8JztQppnyC7AD9QEEoRPms3D63xV1igIyPclTuXDeQ7xEa/rEf24/Po=", "", true, 0, new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "Flearn" },
                    { new Guid("78810ba1-4024-496a-8559-e1a6cc961796"), new DateTime(1991, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "staff.japanese@flearn.com", "Education", "Japanese Language Support", true, "Japanese Language Staff", new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), false, "ZRcdwTwrn8Bbt6y1vSARP5QYQNnd/Dm9wAUpA8PeezqMmrHUKzpO0b2QDVVmwLiOYZGJ2ssgQxmsVuddt3zSwQ==", "hOZyKTEMuf27/ZR2zX1YwD8fudACHEytvlCFEj0ox88StE9CXPqq6rjL/cQ6SuohnVUcTzANOCh73LbXXKHvKm2CqfnTUbN1gGx33u/kTf2OSbVu3bGqtbGnZIuEKfzPLP6r3nKD6kozD/ac7PMvxrCFvlmuzw+q7Y/UQjVRox8=", "", true, 0, new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "StaffJP" },
                    { new Guid("ea0070a9-63cb-4c3f-adc8-98f0c1665ae9"), new DateTime(1992, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "staff.english@flearn.com", "Education", "English Language Support", true, "English Language Staff", new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), false, "ZRcdwTwrn8Bbt6y1vSARP5QYQNnd/Dm9wAUpA8PeezqMmrHUKzpO0b2QDVVmwLiOYZGJ2ssgQxmsVuddt3zSwQ==", "hOZyKTEMuf27/ZR2zX1YwD8fudACHEytvlCFEj0ox88StE9CXPqq6rjL/cQ6SuohnVUcTzANOCh73LbXXKHvKm2CqfnTUbN1gGx33u/kTf2OSbVu3bGqtbGnZIuEKfzPLP6r3nKD6kozD/ac7PMvxrCFvlmuzw+q7Y/UQjVRox8=", "", true, 0, new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "StaffEN" },
                    { new Guid("fb9c94fb-5a2c-4cc1-acc1-117bf55cff1d"), new DateTime(1993, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "staff.chinese@flearn.com", "Education", "Chinese Language Support", true, "Chinese Language Staff", new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), false, "ZRcdwTwrn8Bbt6y1vSARP5QYQNnd/Dm9wAUpA8PeezqMmrHUKzpO0b2QDVVmwLiOYZGJ2ssgQxmsVuddt3zSwQ==", "hOZyKTEMuf27/ZR2zX1YwD8fudACHEytvlCFEj0ox88StE9CXPqq6rjL/cQ6SuohnVUcTzANOCh73LbXXKHvKm2CqfnTUbN1gGx33u/kTf2OSbVu3bGqtbGnZIuEKfzPLP6r3nKD6kozD/ac7PMvxrCFvlmuzw+q7Y/UQjVRox8=", "", true, 0, new DateTime(2025, 9, 25, 1, 0, 42, 862, DateTimeKind.Utc).AddTicks(3926), "StaffZH" }
                });

            migrationBuilder.InsertData(
                table: "UserLanguages",
                columns: new[] { "UserLearningLanguageID", "LanguageID", "UserID" },
                values: new object[,]
                {
                    { new Guid("1db457d9-14da-41d7-9f8c-30927a1c8ebb"), new Guid("b67234c2-8d16-42d4-b700-9e43f78ad841"), new Guid("fb9c94fb-5a2c-4cc1-acc1-117bf55cff1d") },
                    { new Guid("6be8629e-1d66-44ac-852c-cf2d90b90504"), new Guid("e1e28714-3c7c-445c-aef4-00b9e2f2155d"), new Guid("ea0070a9-63cb-4c3f-adc8-98f0c1665ae9") },
                    { new Guid("d764a3d9-89ee-4859-a88e-ee648a246d64"), new Guid("8bfafd1d-dd69-417c-b0a3-ba7cecf6ed52"), new Guid("78810ba1-4024-496a-8559-e1a6cc961796") }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "UserRoleID", "RoleID", "UserID" },
                values: new object[,]
                {
                    { new Guid("1f6377da-d54e-48f5-abbe-936eff3b2fb4"), new Guid("dc286a49-f003-424b-a21d-e654ebcfe2ee"), new Guid("78810ba1-4024-496a-8559-e1a6cc961796") },
                    { new Guid("50a3122e-e3ea-4aa6-ac10-b77073004163"), new Guid("1b5ab4fb-f440-4f90-8a52-de444bddf2cb"), new Guid("08324f9a-3ad0-4524-9c60-9021e852ba30") },
                    { new Guid("7008e482-4d37-4439-a9a3-215d0fe9ab3a"), new Guid("dc286a49-f003-424b-a21d-e654ebcfe2ee"), new Guid("ea0070a9-63cb-4c3f-adc8-98f0c1665ae9") },
                    { new Guid("a8563ab3-6313-483c-9bd6-d21938b0b1d4"), new Guid("dc286a49-f003-424b-a21d-e654ebcfe2ee"), new Guid("fb9c94fb-5a2c-4cc1-acc1-117bf55cff1d") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("08c70a59-16b2-488a-8a92-16b7e6082ab4"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("551cee5e-59dc-404b-a916-1c5ab6bdcb0e"));

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "AchievementID",
                keyValue: new Guid("cf676fa1-18e3-413d-a4a0-c337666a9d4e"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("8bfafd1d-dd69-417c-b0a3-ba7cecf6ed52"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("b67234c2-8d16-42d4-b700-9e43f78ad841"));

            migrationBuilder.DeleteData(
                table: "Languages",
                keyColumn: "LanguageID",
                keyValue: new Guid("e1e28714-3c7c-445c-aef4-00b9e2f2155d"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("1b49d836-11f2-4ce6-8d6e-aa0571a226ce"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("9b9ce2a4-be37-4bc2-820c-4beffa649122"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("58fcd562-594d-4a1b-8da3-7b27e7f8c115"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("849a2b06-8116-4031-8f80-ee1451538cb6"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("c93a32fd-6cad-486a-bba8-e8825d2b04d9"));

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "TopicID",
                keyValue: new Guid("cc4c4592-8eeb-4e78-bf96-9f921b00bf33"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("1db457d9-14da-41d7-9f8c-30927a1c8ebb"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("6be8629e-1d66-44ac-852c-cf2d90b90504"));

            migrationBuilder.DeleteData(
                table: "UserLanguages",
                keyColumn: "UserLearningLanguageID",
                keyValue: new Guid("d764a3d9-89ee-4859-a88e-ee648a246d64"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("1f6377da-d54e-48f5-abbe-936eff3b2fb4"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("50a3122e-e3ea-4aa6-ac10-b77073004163"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("7008e482-4d37-4439-a9a3-215d0fe9ab3a"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "UserRoleID",
                keyValue: new Guid("a8563ab3-6313-483c-9bd6-d21938b0b1d4"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("1b5ab4fb-f440-4f90-8a52-de444bddf2cb"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleID",
                keyValue: new Guid("dc286a49-f003-424b-a21d-e654ebcfe2ee"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("08324f9a-3ad0-4524-9c60-9021e852ba30"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("78810ba1-4024-496a-8559-e1a6cc961796"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("ea0070a9-63cb-4c3f-adc8-98f0c1665ae9"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: new Guid("fb9c94fb-5a2c-4cc1-acc1-117bf55cff1d"));

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Topics");

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "AchievementID", "Critertia", "Description", "IconUrl", "Title" },
                values: new object[,]
                {
                    { new Guid("23b7dcc1-329e-43ee-ad9e-e968fe241aa6"), "7 consecutive days of learning", "Maintain a 7-day learning streak", "🔥", "Week Warrior" },
                    { new Guid("e0d226bc-e3d7-40cf-9f45-44203de634a1"), "Complete 1 course", "Complete your first course", "🏆", "Course Completion" },
                    { new Guid("fe49651c-37e7-4382-bc47-2de2c25c816d"), "Complete 1 lesson", "Complete your first lesson", "🎯", "First Steps" }
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "LanguageID", "CreateAt", "LanguageCode", "LanguageName" },
                values: new object[,]
                {
                    { new Guid("2c5bd7be-5ea2-4c7b-b60b-c6c9300eadfb"), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "EN", "English" },
                    { new Guid("7165b3a2-08db-4cb4-bd56-576b39e91306"), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "JP", "Japanese" },
                    { new Guid("e6b5e7d4-661f-43f1-8bd0-006d2553b94f"), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "ZH", "Chinese" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("19e4f171-3d4e-4d9f-a811-bd0bd0e08cdb"), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "Staff member for specific language support", "Staff" },
                    { new Guid("61e85b5f-99be-4501-9b8d-1394211faeec"), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "Student learning languages", "Learner" },
                    { new Guid("d310ae94-23f1-487d-a62d-cf6b5b7a1d91"), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "System administrator with full access", "Admin" },
                    { new Guid("fdb64d34-dd07-4220-88ac-e95d73fa4d66"), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "Teacher who can create and manage courses", "Teacher" }
                });

            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "TopicID", "Description", "ImageUrl", "Name" },
                values: new object[,]
                {
                    { new Guid("43550bfb-9f88-44a8-9e67-0076527a9d4e"), "Basic and advanced grammar concepts", null, "Grammar" },
                    { new Guid("8b858937-3f09-4044-ac7a-28e0292aad1d"), "Pronunciation and speaking skills", null, "Pronunciation" },
                    { new Guid("9c8fed74-95dc-452f-b5eb-7f3be4d67555"), "Practical conversation skills and dialogues", null, "Conversation" },
                    { new Guid("d1ba7c2c-e9e8-4545-9c2e-effa21390e21"), "Essential vocabulary for daily communication", null, "Vocabulary" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "BirthDate", "CreatedAt", "Email", "Industry", "Interests", "IsEmailConfirmed", "JobTitle", "LastAcessAt", "MfaEnabled", "PasswordHash", "PasswordSalt", "ProfilePictureUrl", "Status", "StreakDays", "UpdateAt", "UserName" },
                values: new object[,]
                {
                    { new Guid("059b4949-2d15-4509-b05f-2b9e3162a5d7"), new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "admin@flearn.com", "Education Technology", "System Management", true, "System Administrator", new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), false, "IT2i/qo6eOJipLpZWLZVPAr+oIrjHxErUBCJyj6Ayy2ktB6Q1Zi4EaLN3IiGPcqLNVtEO5HJ9ynea2JNsnlqEg==", "bhEQN393tDcGwgCb9xSuS4QR1OpEjE4P4mUp+mLnQDw8q7IIBJbgsM8Tk0gQ/iK5PQrisSU4FNSYT8Yaliq8xt9G8hKOvnExtfmGmhl2FC9RajZeeZgDYuRBR76aa03jQ68JEmRuJkQF1mBMsbikMUJ+dAKF11j+x0mLfEU1a1o=", "", true, 0, new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "Flearn" },
                    { new Guid("86668e44-9695-4f45-b053-5bf3b35e67f9"), new DateTime(1992, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "staff.english@flearn.com", "Education", "English Language Support", true, "English Language Staff", new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), false, "UK+aFFG7MiQhxM9DXOCfu/hYtJ7tuomktSlRIyAvNHgR1Rzm/C/B/n2II7pxa39Ee/9+py/p93HG9OlvfzKSXw==", "239Ia5gIZ/TiTWey1zNbo6UXNq5WXD2UaAwe+YP2CLjc7KZHge3+Gj47p8Xxr9wyjZIUhcRULdODMWZ2002VQHmXCY8PlgTMISZAldqhaLl6pvK63EsL6Oa9LUA2hqSBv+2TlXAhozEAt/rU52WdgGn01a5AEMgZ47/CgoUepIw=", "", true, 0, new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "StaffEN" },
                    { new Guid("94cac57f-f19f-4726-b4e7-ccb075984987"), new DateTime(1991, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "staff.japanese@flearn.com", "Education", "Japanese Language Support", true, "Japanese Language Staff", new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), false, "UK+aFFG7MiQhxM9DXOCfu/hYtJ7tuomktSlRIyAvNHgR1Rzm/C/B/n2II7pxa39Ee/9+py/p93HG9OlvfzKSXw==", "239Ia5gIZ/TiTWey1zNbo6UXNq5WXD2UaAwe+YP2CLjc7KZHge3+Gj47p8Xxr9wyjZIUhcRULdODMWZ2002VQHmXCY8PlgTMISZAldqhaLl6pvK63EsL6Oa9LUA2hqSBv+2TlXAhozEAt/rU52WdgGn01a5AEMgZ47/CgoUepIw=", "", true, 0, new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "StaffJP" },
                    { new Guid("dac2f21d-5f96-4e3e-b720-22d0d810a19d"), new DateTime(1993, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "staff.chinese@flearn.com", "Education", "Chinese Language Support", true, "Chinese Language Staff", new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), false, "UK+aFFG7MiQhxM9DXOCfu/hYtJ7tuomktSlRIyAvNHgR1Rzm/C/B/n2II7pxa39Ee/9+py/p93HG9OlvfzKSXw==", "239Ia5gIZ/TiTWey1zNbo6UXNq5WXD2UaAwe+YP2CLjc7KZHge3+Gj47p8Xxr9wyjZIUhcRULdODMWZ2002VQHmXCY8PlgTMISZAldqhaLl6pvK63EsL6Oa9LUA2hqSBv+2TlXAhozEAt/rU52WdgGn01a5AEMgZ47/CgoUepIw=", "", true, 0, new DateTime(2025, 9, 25, 0, 36, 47, 243, DateTimeKind.Utc).AddTicks(6148), "StaffZH" }
                });

            migrationBuilder.InsertData(
                table: "UserLanguages",
                columns: new[] { "UserLearningLanguageID", "LanguageID", "UserID" },
                values: new object[,]
                {
                    { new Guid("50dd59db-85d8-495d-8970-81420811a5ec"), new Guid("e6b5e7d4-661f-43f1-8bd0-006d2553b94f"), new Guid("dac2f21d-5f96-4e3e-b720-22d0d810a19d") },
                    { new Guid("e401f49e-8ce5-4654-90af-71cd06852f9e"), new Guid("2c5bd7be-5ea2-4c7b-b60b-c6c9300eadfb"), new Guid("86668e44-9695-4f45-b053-5bf3b35e67f9") },
                    { new Guid("f1ab46cd-0c8a-4724-9f83-169cdf50a462"), new Guid("7165b3a2-08db-4cb4-bd56-576b39e91306"), new Guid("94cac57f-f19f-4726-b4e7-ccb075984987") }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "UserRoleID", "RoleID", "UserID" },
                values: new object[,]
                {
                    { new Guid("25573809-51f6-45b5-9421-2f88a4426caf"), new Guid("19e4f171-3d4e-4d9f-a811-bd0bd0e08cdb"), new Guid("86668e44-9695-4f45-b053-5bf3b35e67f9") },
                    { new Guid("28aa53f6-b3ff-4e37-9d5a-524f069a0141"), new Guid("19e4f171-3d4e-4d9f-a811-bd0bd0e08cdb"), new Guid("dac2f21d-5f96-4e3e-b720-22d0d810a19d") },
                    { new Guid("c4017a37-6c51-4d41-a09c-27c4ba27eab6"), new Guid("19e4f171-3d4e-4d9f-a811-bd0bd0e08cdb"), new Guid("94cac57f-f19f-4726-b4e7-ccb075984987") },
                    { new Guid("fc087ce1-f6d3-4963-84d0-2fcce6bcf9f3"), new Guid("d310ae94-23f1-487d-a62d-cf6b5b7a1d91"), new Guid("059b4949-2d15-4509-b05f-2b9e3162a5d7") }
                });
        }
    }
}
