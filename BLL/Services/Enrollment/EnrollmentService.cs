//using BLL.IServices.Enrollment;
//using Common.DTO.ApiResponse;
//using Common.DTO.Enrollment.Request;
//using Common.DTO.Enrollment.Response;
//using Common.DTO.Paging.Request;
//using Common.DTO.Paging.Response;
//using DAL.Helpers;
//using DAL.Type;
//using DAL.UnitOfWork;
//using Microsoft.EntityFrameworkCore;

//namespace BLL.Services.Enrollment
//{
//    public class EnrollmentService : IEnrollmentService
//    {
//        private readonly IUnitOfWork _unit;
//        public EnrollmentService(IUnitOfWork unit)
//        {
//            _unit = unit;
//        }
//        public async Task<BaseResponse<EnrollmentResponse>> EnrolCourseAsync(Guid userId, EnrollmentRequest request)
//        {
//            try
//            {
//                var user = await _unit.Users.GetByIdAsync(userId);
//                if (user == null)
//                    return BaseResponse<EnrollmentResponse>.Fail(null, "Unauthorized", 401);

//                if (!user.Status)
//                    return BaseResponse<EnrollmentResponse>.Fail(null, "Account is inactive", 403);

//                if (!user.IsEmailConfirmed)
//                    return BaseResponse<EnrollmentResponse>.Fail(null, "Email not confirmed", 403);

//                var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);
//                if (learner == null)
//                    return BaseResponse<EnrollmentResponse>.Fail(null, "Unauthorized", 401);

//                var course = await _unit.Courses.GetByIdAsync(request.CourseId);

//                if (course == null)
//                    return BaseResponse<EnrollmentResponse>.Fail(null, "Course not found.", 404);

//                if (course.Status != CourseStatus.Published)
//                    return BaseResponse<EnrollmentResponse>.Fail(null, "This course is not yet published and cannot be enrolled.", 400);

//                if (course.Type == CourseType.Paid)
//                {
//                    bool hasPurchased = await _unit.Courses.HasUserPurchasedCourseAsync(userId, request.CourseId);
//                    if (!hasPurchased)
//                        return BaseResponse<EnrollmentResponse>.Fail(null, "Course not purchased", 403);
//                }

//                var existingEnrollment = await _unit.Enrollments.Query()
//                    .FirstOrDefaultAsync(e => e.CourseId == request.CourseId && e.LearnerId == learner.LearnerLanguageId);


//                if (existingEnrollment != null)
//                    return BaseResponse<EnrollmentResponse>.Fail(null, "You are already enrolled in this course.", 400);


//                var enrollment = new DAL.Models.Enrollment
//                {
//                    EnrollmentID = Guid.NewGuid(),
//                    CourseId = request.CourseId,
//                    LearnerId = learner.LearnerLanguageId,
//                    EnrolledAt = TimeHelper.GetVietnamTime()
//                };

//                await _unit.Enrollments.CreateAsync(enrollment);
//                await _unit.SaveChangesAsync();

//                var teacher = await _unit.TeacherProfiles.GetByIdAsync(course.TeacherId);
//                var language = await _unit.Languages.GetByIdAsync(course.LanguageId);
//                var topics = await _unit.CourseTopics.Query()
//                    .OrderBy(ct => ct.CreatedAt)
//                    .Include(ct => ct.Topic)
//                    .Where(ct => ct.CourseID == course.CourseID)
//                    .ToListAsync();

//                var enrollmentResponse = new EnrollmentResponse
//                {
//                    EnrollmentID = enrollment.EnrollmentID,
//                    CourseId = course.CourseID,
//                    LearnerId = learner.LearnerLanguageId,
//                    EnrolledAt = enrollment.EnrolledAt.ToString("yyyy-MM-dd HH:mm:ss"),
//                    Status = "Active",
//                    Course = new CourseBasicInfo
//                    {
//                        CourseID = course.CourseID,
//                        Title = course.Title,
//                        ImageUrl = course.ImageUrl,
//                        Price = course.Price,
//                        CourseType = course.Type.ToString(),
//                        CourseLevel = course.Level.ToString(),
//                        Status = course.Status.ToString()
//                    }
//                };

//                return BaseResponse<EnrollmentResponse>.Success(enrollmentResponse, "Enrolled successfully.");
//            }
//            catch (Exception ex)
//            {
//                return BaseResponse<EnrollmentResponse>.Error($"Unexpected error occurred: {ex.Message}", 500, null);
//            }
//        }
//        public async Task<PagedResponse<IEnumerable<EnrollmentResponse>>> GetEnrolledCoursesAsync(Guid userId, string lang, PagingRequest request)
//        {
//            var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);
//            if (learner == null)
//                return PagedResponse<IEnumerable<EnrollmentResponse>>.Fail(null, "Unauthorized", 401);

//            var query = _unit.Enrollments.Query()
//                .Include(e => e.Course)
//                    .ThenInclude(c => c.Teacher)
//                .Include(e => e.Course)
//                    .ThenInclude(c => c.Language)
//                .Where(e => e.LearnerId == learner.LearnerLanguageId);

//            if (!string.IsNullOrEmpty(lang))
//            {
//                query = query.Where(e => e.Course.Language.LanguageCode == lang);
//            }

//            query = query.OrderByDescending(e => e.EnrolledAt);

//            int totalItems = await query.CountAsync();
//            var enrollments = await query
//                .Skip((request.Page - 1) * request.PageSize)
//                .Take(request.PageSize)
//                .ToListAsync();

//            var enrollmentResponses = enrollments.Select(e => new EnrollmentResponse
//            {
//                EnrollmentID = e.EnrollmentID,
//                LearnerId = e.LearnerId,
//                CourseId = e.CourseId,
//                EnrolledAt = e.EnrolledAt.ToString("yyyy-MM-dd HH:mm:ss"),
//                Status = e.Status.ToString(),
//                CompletedLessons = 0,
//                ProgressPercent = e.ProgressPercent,
//                TotalLessons = e.Course.NumLessons,
//                Course = e.Course != null ? new CourseBasicInfo
//                {
//                    CourseID = e.Course.CourseID,
//                    Title = e.Course.Title,
//                    ImageUrl = e.Course.ImageUrl,
//                    Price = e.Course.Price,
//                    CourseType = e.Course.Type.ToString(),
//                    CourseLevel = e.Course.Level.ToString(),
//                    Status = e.Course.Status.ToString(),
//                    LanguageCode = e.Course.Language.LanguageCode,
//                    TeacherInfo = e.Course.Teacher != null ? new TeacherInfo
//                    {
//                        TeacherId = e.Course.Teacher.TeacherProfileId,
//                        Avatar = e.Course.Teacher.Avatar,
//                        Email = e.Course.Teacher.Email,
//                        FullName = e.Course.Teacher.FullName,
//                        PhoneNumber = e.Course.Teacher.PhoneNumber,
//                    } : new TeacherInfo()
//                } : new CourseBasicInfo()
//            }).ToList();

//            return PagedResponse<IEnumerable<EnrollmentResponse>>.Success(enrollmentResponses, request.Page, request.PageSize, totalItems, "Success");
//        }
//    }
//}
