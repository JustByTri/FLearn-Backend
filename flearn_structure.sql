-- MySQL dump 10.13  Distrib 8.0.43, for Linux (x86_64)
--
-- Host: gateway01.ap-southeast-1.prod.aws.tidbcloud.com    Database: FLearn
-- ------------------------------------------------------
-- Server version	8.0.11-TiDB-v7.5.2-serverless

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `AIFeedBack`
--

DROP TABLE IF EXISTS `AIFeedBacks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AIFeedBacks` (
  `AIFeedBackID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `ConversationID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Content` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `FeedbackText` longtext COLLATE utf8mb4_general_ci DEFAULT NULL,
  `FluencyScore` int(11) NOT NULL,
  `PronunciationScore` int(11) NOT NULL,
  `GrammarScore` int(11) NOT NULL,
  `VocabularyScore` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci DEFAULT NULL,
  PRIMARY KEY (`AIFeedBackID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_AIFeedBacks_Conversations_ConversationID` (`ConversationID`),
  KEY `IX_AIFeedBacks_ConversationID` (`ConversationID`),
  CONSTRAINT `FK_AIFeedBacks_Conversations_ConversationID` FOREIGN KEY (`ConversationID`) REFERENCES `FLearn`.`Conversations` (`ConversationID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Achievements`
--

DROP TABLE IF EXISTS `Achievements`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Achievements` (
  `AchievementID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Title` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(500) COLLATE utf8mb4_general_ci NOT NULL,
  `IconUrl` longtext COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Critertia` longtext COLLATE utf8mb4_general_ci DEFAULT NULL,
  PRIMARY KEY (`AchievementID`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Conversations`
--

DROP TABLE IF EXISTS `Conversations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Conversations` (
  `ConversationID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `LanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `StartedAt` datetime(6) NOT NULL,
  `EndedAt` datetime(6) DEFAULT NULL,
  `AIFeedBackID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Topic` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`ConversationID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_Conversations_Languages_LanguageID` (`LanguageID`),
  KEY `FK_Conversations_Users_UserID` (`UserID`),
  KEY `IX_Conversations_LanguageID` (`LanguageID`),
  KEY `IX_Conversations_UserID` (`UserID`),
  CONSTRAINT `FK_Conversations_Languages_LanguageID` FOREIGN KEY (`LanguageID`) REFERENCES `FLearn`.`Languages` (`LanguageID`) ON DELETE CASCADE,
  CONSTRAINT `FK_Conversations_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `CourseSubmissions`
--

DROP TABLE IF EXISTS `CourseSubmissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `CourseSubmissions` (
  `CourseSubmissionID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `SubmittedBy` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `SubmitterUserID` char(36) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `SubmittedAt` datetime(6) NOT NULL,
  `CourseID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Status` int(11) NOT NULL,
  `ReviewBy` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `ReviewComment` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `ReviewedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`CourseSubmissionID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_CourseSubmissions_Courses_CourseID` (`CourseID`),
  KEY `FK_CourseSubmissions_Users_SubmitterUserID` (`SubmitterUserID`),
  KEY `IX_CourseSubmissions_CourseID` (`CourseID`),
  KEY `IX_CourseSubmissions_SubmitterUserID` (`SubmitterUserID`),
  CONSTRAINT `FK_CourseSubmissions_Courses_CourseID` FOREIGN KEY (`CourseID`) REFERENCES `FLearn`.`Courses` (`CourseID`) ON DELETE CASCADE,
  CONSTRAINT `FK_CourseSubmissions_Users_SubmitterUserID` FOREIGN KEY (`SubmitterUserID`) REFERENCES `FLearn`.`Users` (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `CourseTopics`
--

DROP TABLE IF EXISTS `CourseTopics`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `CourseTopics` (
  `CourseTopicID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CourseID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `TopicID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`CourseTopicID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_CourseTopics_Courses_CourseID` (`CourseID`),
  KEY `FK_CourseTopics_Topics_TopicID` (`TopicID`),
  KEY `IX_CourseTopics_CourseID` (`CourseID`),
  UNIQUE KEY `IX_CourseTopics_TopicID` (`TopicID`),
  CONSTRAINT `FK_CourseTopics_Courses_CourseID` FOREIGN KEY (`CourseID`) REFERENCES `FLearn`.`Courses` (`CourseID`) ON DELETE CASCADE,
  CONSTRAINT `FK_CourseTopics_Topics_TopicID` FOREIGN KEY (`TopicID`) REFERENCES `FLearn`.`Topics` (`TopicID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `CourseUnits`
--

DROP TABLE IF EXISTS `CourseUnits`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `CourseUnits` (
  `CourseUnitID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Title` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(500) COLLATE utf8mb4_general_ci NOT NULL,
  `Position` int(11) NOT NULL,
  `CourseID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`CourseUnitID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_CourseUnits_Courses_CourseID` (`CourseID`),
  KEY `IX_CourseUnits_CourseID` (`CourseID`),
  CONSTRAINT `FK_CourseUnits_Courses_CourseID` FOREIGN KEY (`CourseID`) REFERENCES `FLearn`.`Courses` (`CourseID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Courses`
--

DROP TABLE IF EXISTS `Courses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Courses` (
  `CourseID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Title` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `CoverImageUrl` varchar(300) COLLATE utf8mb4_general_ci NOT NULL,
  `Price` decimal(65,30) NOT NULL,
  `TeacherID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `LanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  `Goal` varchar(500) COLLATE utf8mb4_general_ci NOT NULL,
  `Level` varchar(50) COLLATE utf8mb4_general_ci NOT NULL,
  `SkillFocus` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `PublishedAt` datetime(6) DEFAULT NULL,
  `NumLessons` int(11) NOT NULL,
  `ApprovedByID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Status` int(11) NOT NULL,
  PRIMARY KEY (`CourseID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_Courses_Languages_LanguageID` (`LanguageID`),
  KEY `FK_Courses_Users_TeacherID` (`TeacherID`),
  KEY `IX_Courses_LanguageID` (`LanguageID`),
  KEY `IX_Courses_TeacherID` (`TeacherID`),
  CONSTRAINT `FK_Courses_Languages_LanguageID` FOREIGN KEY (`LanguageID`) REFERENCES `FLearn`.`Languages` (`LanguageID`) ON DELETE CASCADE,
  CONSTRAINT `FK_Courses_Users_TeacherID` FOREIGN KEY (`TeacherID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Enrollments`
--

DROP TABLE IF EXISTS `Enrollments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Enrollments` (
  `EnrollmentID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CourseID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `EnrolledAt` datetime(6) NOT NULL,
  `CompletedAt` datetime(6) DEFAULT NULL,
  `Progress` decimal(65,30) NOT NULL,
  `IsActive` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`EnrollmentID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_Enrollments_Courses_CourseID` (`CourseID`),
  KEY `IX_Enrollments_CourseID` (`CourseID`),
  CONSTRAINT `FK_Enrollments_Courses_CourseID` FOREIGN KEY (`CourseID`) REFERENCES `FLearn`.`Courses` (`CourseID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Exercises`
--

DROP TABLE IF EXISTS `Exercises`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Exercises` (
  `ExerciseID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Hints` varchar(500) COLLATE utf8mb4_general_ci NOT NULL,
  `Position` int(11) NOT NULL,
  `Materials` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `ExpectedAnswer` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `Prompt` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `LessonID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Type` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  `Title` longtext COLLATE utf8mb4_general_ci NOT NULL,
  `Content` longtext COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`ExerciseID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_Exercises_Lessons_LessonID` (`LessonID`),
  KEY `IX_Exercises_LessonID` (`LessonID`),
  CONSTRAINT `FK_Exercises_Lessons_LessonID` FOREIGN KEY (`LessonID`) REFERENCES `FLearn`.`Lessons` (`LessonID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `LanguageUser`
--

DROP TABLE IF EXISTS `LanguageUser`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `LanguageUser` (
  `LanguagesLanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UsersUserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`LanguagesLanguageID`,`UsersUserID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_LanguageUser_Users_UsersUserID` (`UsersUserID`),
  KEY `IX_LanguageUser_UsersUserID` (`UsersUserID`),
  CONSTRAINT `FK_LanguageUser_Languages_LanguagesLanguageID` FOREIGN KEY (`LanguagesLanguageID`) REFERENCES `FLearn`.`Languages` (`LanguageID`) ON DELETE CASCADE,
  CONSTRAINT `FK_LanguageUser_Users_UsersUserID` FOREIGN KEY (`UsersUserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Languages`
--

DROP TABLE IF EXISTS `Languages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Languages` (
  `LanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `LanguageName` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `LanguageCode` varchar(10) COLLATE utf8mb4_general_ci NOT NULL,
  `CreateAt` datetime(6) NOT NULL,
  PRIMARY KEY (`LanguageID`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Lessons`
--

DROP TABLE IF EXISTS `Lessons`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Lessons` (
  `LessonID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Title` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `Content` varchar(2000) COLLATE utf8mb4_general_ci NOT NULL,
  `Position` int(11) NOT NULL,
  `SkillFocus` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(500) COLLATE utf8mb4_general_ci NOT NULL,
  `IsPublished` datetime(6) NOT NULL,
  `CourseUnitID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CreateAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`LessonID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_Lessons_CourseUnits_CourseUnitID` (`CourseUnitID`),
  KEY `IX_Lessons_CourseUnitID` (`CourseUnitID`),
  CONSTRAINT `FK_Lessons_CourseUnits_CourseUnitID` FOREIGN KEY (`CourseUnitID`) REFERENCES `FLearn`.`CourseUnits` (`CourseUnitID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PasswordResetOtps`
--

DROP TABLE IF EXISTS `PasswordResetOtps`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `PasswordResetOtps` (
  `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Email` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `OtpCode` varchar(6) COLLATE utf8mb4_general_ci NOT NULL,
  `ExpireAt` datetime(6) NOT NULL,
  `IsUsed` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `FailedAttempts` int(11) NOT NULL DEFAULT '0',
  `IpAddress` varchar(45) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `UsedAt` datetime(6) DEFAULT NULL,
  `UserAgent` varchar(500) COLLATE utf8mb4_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Purchases`
--

DROP TABLE IF EXISTS `Purchases`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Purchases` (
  `PurchasesID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `PurchasedAt` datetime(6) NOT NULL,
  `Amount` decimal(65,30) NOT NULL,
  `PaymentMethod` longtext COLLATE utf8mb4_general_ci DEFAULT NULL,
  PRIMARY KEY (`PurchasesID`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PurchasesDetails`
--

DROP TABLE IF EXISTS `PurchasesDetails`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `PurchasesDetails` (
  `PurchasesDetailID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `PurchasesID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CourseID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Amount` decimal(65,30) NOT NULL,
  PRIMARY KEY (`PurchasesDetailID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_PurchasesDetails_Courses_CourseID` (`CourseID`),
  KEY `FK_PurchasesDetails_Purchases_PurchasesID` (`PurchasesID`),
  KEY `IX_PurchasesDetails_CourseID` (`CourseID`),
  KEY `IX_PurchasesDetails_PurchasesID` (`PurchasesID`),
  CONSTRAINT `FK_PurchasesDetails_Courses_CourseID` FOREIGN KEY (`CourseID`) REFERENCES `FLearn`.`Courses` (`CourseID`) ON DELETE CASCADE,
  CONSTRAINT `FK_PurchasesDetails_Purchases_PurchasesID` FOREIGN KEY (`PurchasesID`) REFERENCES `FLearn`.`Purchases` (`PurchasesID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Recordings`
--

DROP TABLE IF EXISTS `Recordings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Recordings` (
  `RecordingID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Url` longtext COLLATE utf8mb4_general_ci DEFAULT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `LanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `FilePath` varchar(300) COLLATE utf8mb4_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `Duration` datetime(6) DEFAULT NULL,
  `ConverationID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `ConversationID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Format` longtext COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`RecordingID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_Recordings_Conversations_ConversationID` (`ConversationID`),
  KEY `FK_Recordings_Languages_LanguageID` (`LanguageID`),
  KEY `IX_Recordings_ConversationID` (`ConversationID`),
  KEY `IX_Recordings_LanguageID` (`LanguageID`),
  CONSTRAINT `FK_Recordings_Conversations_ConversationID` FOREIGN KEY (`ConversationID`) REFERENCES `FLearn`.`Conversations` (`ConversationID`) ON DELETE CASCADE,
  CONSTRAINT `FK_Recordings_Languages_LanguageID` FOREIGN KEY (`LanguageID`) REFERENCES `FLearn`.`Languages` (`LanguageID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `RefreshTokens`
--

DROP TABLE IF EXISTS `RefreshTokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `RefreshTokens` (
  `RefreshTokenID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Token` varchar(500) COLLATE utf8mb4_general_ci NOT NULL,
  `ExpiresAt` datetime(6) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `IsRevoked` tinyint(1) NOT NULL,
  `RevokedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`RefreshTokenID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_RefreshTokens_Users_UserID` (`UserID`),
  UNIQUE KEY `IX_RefreshTokens_Token` (`Token`),
  KEY `IX_RefreshTokens_UserID` (`UserID`),
  CONSTRAINT `FK_RefreshTokens_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `RegistrationOtps`
--

DROP TABLE IF EXISTS `RegistrationOtps`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `RegistrationOtps` (
  `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Email` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `OtpCode` varchar(6) COLLATE utf8mb4_general_ci NOT NULL,
  `ExpireAt` datetime(6) NOT NULL,
  `IsUsed` tinyint(1) NOT NULL,
  `CreateAt` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00.000000',
  PRIMARY KEY (`Id`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Reports`
--

DROP TABLE IF EXISTS `Reports`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Reports` (
  `ReportID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `ReportedUserID` char(36) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ReportedCourseID` char(36) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ReportedLessonID` char(36) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Reason` longtext COLLATE utf8mb4_general_ci NOT NULL,
  `Description` longtext COLLATE utf8mb4_general_ci NOT NULL,
  `ReportedAt` datetime(6) NOT NULL,
  `Status` int(11) NOT NULL,
  `ReviewedAt` datetime(6) DEFAULT NULL,
  `ResolvedBy` char(36) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ReviewComment` longtext COLLATE utf8mb4_general_ci NOT NULL,
  `CreateAt` datetime(6) NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Content` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`ReportID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_Reports_Courses_ReportedCourseID` (`ReportedCourseID`),
  KEY `FK_Reports_Lessons_ReportedLessonID` (`ReportedLessonID`),
  KEY `FK_Reports_Users_ReportedUserID` (`ReportedUserID`),
  KEY `FK_Reports_Users_UserID` (`UserID`),
  KEY `IX_Reports_ReportedCourseID` (`ReportedCourseID`),
  KEY `IX_Reports_ReportedLessonID` (`ReportedLessonID`),
  KEY `IX_Reports_ReportedUserID` (`ReportedUserID`),
  KEY `IX_Reports_UserID` (`UserID`),
  CONSTRAINT `FK_Reports_Courses_ReportedCourseID` FOREIGN KEY (`ReportedCourseID`) REFERENCES `FLearn`.`Courses` (`CourseID`),
  CONSTRAINT `FK_Reports_Lessons_ReportedLessonID` FOREIGN KEY (`ReportedLessonID`) REFERENCES `FLearn`.`Lessons` (`LessonID`),
  CONSTRAINT `FK_Reports_Users_ReportedUserID` FOREIGN KEY (`ReportedUserID`) REFERENCES `FLearn`.`Users` (`UserID`),
  CONSTRAINT `FK_Reports_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `RoadmapDetails`
--

DROP TABLE IF EXISTS `RoadmapDetails`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `RoadmapDetails` (
  `RoadmapDetailID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `RoadmapID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `StepNumber` int(11) NOT NULL,
  `Title` varchar(300) COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `Skills` varchar(500) COLLATE utf8mb4_general_ci NOT NULL,
  `Resources` longtext COLLATE utf8mb4_general_ci DEFAULT NULL,
  `EstimatedHours` int(11) NOT NULL,
  `DifficultyLevel` varchar(50) COLLATE utf8mb4_general_ci NOT NULL,
  `IsCompleted` tinyint(1) NOT NULL,
  `CompletedAt` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`RoadmapDetailID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_RoadmapDetails_Roadmaps_RoadmapID` (`RoadmapID`),
  UNIQUE KEY `IX_RoadmapDetails_RoadmapID_StepNumber` (`RoadmapID`,`StepNumber`),
  CONSTRAINT `FK_RoadmapDetails_Roadmaps_RoadmapID` FOREIGN KEY (`RoadmapID`) REFERENCES `FLearn`.`Roadmaps` (`RoadmapID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Roadmaps`
--

DROP TABLE IF EXISTS `Roadmaps`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Roadmaps` (
  `RoadmapID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `LanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Title` varchar(300) COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `CurrentLevel` varchar(50) COLLATE utf8mb4_general_ci NOT NULL,
  `TargetLevel` varchar(50) COLLATE utf8mb4_general_ci NOT NULL,
  `EstimatedDuration` int(11) NOT NULL,
  `DurationUnit` varchar(20) COLLATE utf8mb4_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `Progress` decimal(5,2) NOT NULL,
  PRIMARY KEY (`RoadmapID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_Roadmaps_Languages_LanguageID` (`LanguageID`),
  KEY `FK_Roadmaps_Users_UserID` (`UserID`),
  KEY `IX_Roadmaps_LanguageID` (`LanguageID`),
  KEY `IX_Roadmaps_UserID` (`UserID`),
  CONSTRAINT `FK_Roadmaps_Languages_LanguageID` FOREIGN KEY (`LanguageID`) REFERENCES `FLearn`.`Languages` (`LanguageID`) ON DELETE CASCADE,
  CONSTRAINT `FK_Roadmaps_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `RoleUser`
--

DROP TABLE IF EXISTS `RoleUser`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `RoleUser` (
  `RolesRoleID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UsersUserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`RolesRoleID`,`UsersUserID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_RoleUser_Users_UsersUserID` (`UsersUserID`),
  KEY `IX_RoleUser_UsersUserID` (`UsersUserID`),
  CONSTRAINT `FK_RoleUser_Roles_RolesRoleID` FOREIGN KEY (`RolesRoleID`) REFERENCES `FLearn`.`Roles` (`RoleID`) ON DELETE CASCADE,
  CONSTRAINT `FK_RoleUser_Users_UsersUserID` FOREIGN KEY (`UsersUserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Roles`
--

DROP TABLE IF EXISTS `Roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Roles` (
  `RoleID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Name` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `Description` longtext COLLATE utf8mb4_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`RoleID`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `TeacherApplications`
--

DROP TABLE IF EXISTS `TeacherApplications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `TeacherApplications` (
  `TeacherApplicationID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Motivation` varchar(1000) COLLATE utf8mb4_general_ci NOT NULL,
  `AppliedAt` datetime(6) NOT NULL,
  `SubmitAt` datetime(6) NOT NULL,
  `ReviewAt` datetime(6) NOT NULL,
  `ReviewedBy` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `RejectionReason` longtext COLLATE utf8mb4_general_ci NOT NULL,
  `Status` tinyint(1) NOT NULL,
  `TeacherCredentialID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `LanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `Specialization` longtext COLLATE utf8mb4_general_ci NOT NULL,
  `TeachingExperience` longtext COLLATE utf8mb4_general_ci NOT NULL,
  `TeachingLevel` longtext COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`TeacherApplicationID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_TeacherApplications_Languages_LanguageID` (`LanguageID`),
  KEY `FK_TeacherApplications_Users_UserID` (`UserID`),
  KEY `IX_TeacherApplications_LanguageID` (`LanguageID`),
  KEY `IX_TeacherApplications_UserID` (`UserID`),
  CONSTRAINT `FK_TeacherApplications_Languages_LanguageID` FOREIGN KEY (`LanguageID`) REFERENCES `FLearn`.`Languages` (`LanguageID`) ON DELETE CASCADE,
  CONSTRAINT `FK_TeacherApplications_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `TeacherCredentials`
--

DROP TABLE IF EXISTS `TeacherCredentials`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `TeacherCredentials` (
  `TeacherCredentialID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CredentialName` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `CredentialFileUrl` varchar(300) COLLATE utf8mb4_general_ci NOT NULL,
  `ApplicationID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `Type` int(11) NOT NULL,
  PRIMARY KEY (`TeacherCredentialID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_TeacherCredentials_TeacherApplications_ApplicationID` (`ApplicationID`),
  KEY `FK_TeacherCredentials_Users_UserID` (`UserID`),
  KEY `IX_TeacherCredentials_ApplicationID` (`ApplicationID`),
  KEY `IX_TeacherCredentials_UserID` (`UserID`),
  CONSTRAINT `FK_TeacherCredentials_TeacherApplications_ApplicationID` FOREIGN KEY (`ApplicationID`) REFERENCES `FLearn`.`TeacherApplications` (`TeacherApplicationID`) ON DELETE CASCADE,
  CONSTRAINT `FK_TeacherCredentials_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `TempRegistrations`
--

DROP TABLE IF EXISTS `TempRegistrations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `TempRegistrations` (
  `Id` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Email` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `UserName` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `PasswordHash` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `PasswordSalt` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `OtpCode` varchar(6) COLLATE utf8mb4_general_ci NOT NULL,
  `ExpireAt` datetime(6) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `IsUsed` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Topics`
--

DROP TABLE IF EXISTS `Topics`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Topics` (
  `TopicID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `Name` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(500) COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`TopicID`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `UserAchievements`
--

DROP TABLE IF EXISTS `UserAchievements`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UserAchievements` (
  `UserAchievementID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `AchievementID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `AchievedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`UserAchievementID`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `UserLanguages`
--

DROP TABLE IF EXISTS `UserLanguages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UserLanguages` (
  `UserLearningLanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `LanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`UserLearningLanguageID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_UserLanguages_Users_UserID` (`UserID`),
  KEY `IX_UserLanguages_UserID` (`UserID`),
  CONSTRAINT `FK_UserLanguages_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `UserRoles`
--

DROP TABLE IF EXISTS `UserRoles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UserRoles` (
  `UserRoleID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `RoleID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`UserRoleID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_UserRoles_Roles_RoleID` (`RoleID`),
  KEY `FK_UserRoles_Users_UserID` (`UserID`),
  KEY `IX_UserRoles_RoleID` (`RoleID`),
  KEY `IX_UserRoles_UserID` (`UserID`),
  CONSTRAINT `FK_UserRoles_Roles_RoleID` FOREIGN KEY (`RoleID`) REFERENCES `FLearn`.`Roles` (`RoleID`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserRoles_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `UserSurveys`
--

DROP TABLE IF EXISTS `UserSurveys`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UserSurveys` (
  `SurveyID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `CurrentLevel` varchar(50) COLLATE utf8mb4_general_ci NOT NULL,
  `PreferredLanguageID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `LearningReason` varchar(500) COLLATE utf8mb4_general_ci NOT NULL,
  `PreviousExperience` varchar(300) COLLATE utf8mb4_general_ci NOT NULL,
  `PreferredLearningStyle` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `InterestedTopics` varchar(300) COLLATE utf8mb4_general_ci NOT NULL,
  `PrioritySkills` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `TargetTimeline` varchar(50) COLLATE utf8mb4_general_ci NOT NULL,
  `IsCompleted` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `CompletedAt` datetime(6) DEFAULT NULL,
  `AiRecommendations` text COLLATE utf8mb4_general_ci DEFAULT NULL,
  `ConfidenceLevel` int(11) DEFAULT NULL,
  `PreferredAccent` varchar(200) COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `SpeakingChallenges` varchar(200) COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`SurveyID`) /*T![clustered_index] CLUSTERED */,
  KEY `FK_UserSurveys_Languages_PreferredLanguageID` (`PreferredLanguageID`),
  KEY `FK_UserSurveys_Users_UserID` (`UserID`),
  KEY `IX_UserSurveys_PreferredLanguageID` (`PreferredLanguageID`),
  KEY `IX_UserSurveys_UserID` (`UserID`),
  CONSTRAINT `FK_UserSurveys_Languages_PreferredLanguageID` FOREIGN KEY (`PreferredLanguageID`) REFERENCES `FLearn`.`Languages` (`LanguageID`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserSurveys_Users_UserID` FOREIGN KEY (`UserID`) REFERENCES `FLearn`.`Users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Users`
--

DROP TABLE IF EXISTS `Users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Users` (
  `UserID` char(36) COLLATE utf8mb4_general_ci NOT NULL,
  `UserName` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
  `Email` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `PasswordHash` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `PasswordSalt` varchar(200) COLLATE utf8mb4_general_ci NOT NULL,
  `LastAcessAt` datetime(6) DEFAULT NULL,
  `JobTitle` varchar(100) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `Industry` varchar(100) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `StreakDays` int(11) DEFAULT NULL,
  `Interests` varchar(500) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `BirthDate` datetime(6) DEFAULT NULL,
  `Status` tinyint(1) NOT NULL,
  `UpdateAt` datetime(6) NOT NULL,
  `MfaEnabled` tinyint(1) DEFAULT NULL,
  `ProfilePictureUrl` varchar(300) COLLATE utf8mb4_general_ci DEFAULT NULL,
  `IsEmailConfirmed` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`UserID`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `__EFMigrationsHistory`
--

DROP TABLE IF EXISTS `__EFMigrationsHistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`) /*T![clustered_index] CLUSTERED */
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping routines for database 'FLearn'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-09-27 15:33:52
