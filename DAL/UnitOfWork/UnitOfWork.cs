using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using DAL.Repositories;

using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction _transaction;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new UserRepository(_context);
            Roles = new RoleRepository(_context);
            UserRoles = new UserRoleRepository(_context);
            Languages = new LanguageRepository(_context);
            UserLearningLanguages = new UserLearningLanguageRepository(_context);
            Achievements = new AchievementRepository(_context);
            UserAchievements = new UserAchievementRepository(_context);
            Courses = new CourseRepository(_context);
            CourseUnits = new CourseUnitRepository(_context);
            Lessons = new LessonRepository(_context);
            Exercises = new ExerciseRepository(_context);
            Enrollments = new EnrollmentRepository(_context);
            Purchases = new PurchasesRepository(_context);
            PurchasesDetails = new PurchasesDetailRepository(_context);
            CourseTopics = new CourseTopicRepository(_context);
            Topics = new TopicRepository(_context);
            CourseSubmissions = new CourseSubmissionRepository(_context);
            TeacherApplications = new TeacherApplicationRepository(_context);
            TeacherCredentials = new TeacherCredentialRepository(_context);
            Recordings = new RecordingRepository(_context);
            Reports = new ReportRepository(_context);
            AIFeedBacks = new AIFeedBackRepository(_context);
            Conversations = new ConversationRepository(_context);
            RefreshTokens = new RefreshTokenRepository(_context);
            Roadmaps = new RoadmapRepository(_context);
            RoadmapDetails = new RoadmapDetailRepository(_context);
            RegistrationOtps = new RegistrationOtpRepository(_context);
            TempRegistrations = new TempRegistrationRepository(_context);
            PasswordResetOtps = new PasswordResetOtpRepository(_context);
        }

        public IUserRepository Users { get; private set; }
        public IRoleRepository Roles { get; private set; }
        public IUserRoleRepository UserRoles { get; private set; }
        public ILanguageRepository Languages { get; private set; }
        public IUserLearningLanguageRepository UserLearningLanguages { get; private set; }
        public IAchievementRepository Achievements { get; private set; }
        public IUserAchievementRepository UserAchievements { get; private set; }
        public ICourseRepository Courses { get; private set; }
        public ICourseUnitRepository CourseUnits { get; private set; }
        public ILessonRepository Lessons { get; private set; }
        public IExerciseRepository Exercises { get; private set; }
        public IEnrollmentRepository Enrollments { get; private set; }
        public IPurchasesRepository Purchases { get; private set; }
        public IPurchasesDetailRepository PurchasesDetails { get; private set; }
        public ICourseTopicRepository CourseTopics { get; private set; }
        public ITopicRepository Topics { get; private set; }
        public ICourseSubmissionRepository CourseSubmissions { get; private set; }
        public ITeacherApplicationRepository TeacherApplications { get; private set; }
        public ITeacherCredentialRepository TeacherCredentials { get; private set; }
        public IRecordingRepository Recordings { get; private set; }
        public IReportRepository Reports { get; private set; }
        public IAIFeedBackRepository AIFeedBacks { get; private set; }
        public IConversationRepository Conversations { get; private set; }
        public IRefreshTokenRepository RefreshTokens { get; private set; }
        public IRoadmapRepository Roadmaps { get; private set; }
        public IRoadmapDetailRepository RoadmapDetails { get; private set; }

        public IRegistrationOtpRepository RegistrationOtps { get; private set; }
        public ITempRegistrationRepository TempRegistrations { get; private set; }
        public IPasswordResetOtpRepository PasswordResetOtps { get; private set; }
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void BeginTransaction()
        {
            _transaction = _context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            try
            {
                _context.SaveChanges();
                _transaction?.Commit();
            }
            catch
            {
                _transaction?.Rollback();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}
