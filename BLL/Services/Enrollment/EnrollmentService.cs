using BLL.IServices.Enrollment;
using Common.DTO.ApiResponse;
using Common.DTO.Enrollment.Request;
using Common.DTO.Enrollment.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Enrollment
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _unit;
        public EnrollmentService(IUnitOfWork unit)
        {
            _unit = unit;
        }
        public async Task<BaseResponse<EnrollmentResponse>> EnrolCourseAsync(Guid userId, EnrollmentRequest request)
        {

            await _unit.BeginTransactionAsync();
            try
            {
                var user = await _unit.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Unauthorized", 401);

                if (!user.Status)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Account is inactive", 403);

                if (!user.IsEmailConfirmed)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Email not confirmed", 403);

                var learner = await _unit.LearnerLanguages.FindAsync(l => l.UserId == userId);
                if (learner == null)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Unauthorized", 401);

                var course = await _unit.Courses.GetByIdAsync(request.CourseId);

                if (course == null)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Course not found.", 404);

                if (course.Status != CourseStatus.Published)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "This course is not yet published and cannot be enrolled.", 400);

                if (course.CourseType == CourseType.Paid)
                {
                    bool hasPurchased = await _unit.Courses.HasUserPurchasedCourseAsync(userId, request.CourseId);
                    if (!hasPurchased)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Course not purchased", 403);
                }

                var existingEnrollment = await _unit.Enrollments.Query()
                    .FirstOrDefaultAsync(e => e.CourseId == request.CourseId && e.LearnerId == learner.LearnerLanguageId);


                if (existingEnrollment != null)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "You are already enrolled in this course.", 400);


                var enrollment = new DAL.Models.Enrollment
                {
                    EnrollmentID = Guid.NewGuid(),
                    CourseId = request.CourseId,
                    LearnerId = learner.LearnerLanguageId,
                    EnrolledAt = TimeHelper.GetVietnamTime()
                };

                await _unit.Enrollments.CreateAsync(enrollment);

                var enrollmentResponse = new EnrollmentResponse
                {
                    EnrollmentId = enrollment.EnrollmentID,
                    CourseId = course.CourseID,
                    CourseType = course.CourseType.ToString(),
                    ProgressPercent = enrollment.ProgressPercent,
                    EnrollmentDate = enrollment.EnrolledAt.ToString("dd-MM-yyyy HH:mm"),
                };

                return BaseResponse<EnrollmentResponse>.Success(enrollmentResponse, "Enrolled successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<EnrollmentResponse>.Error($"Unexpected error occurred: {ex.Message}", 500, null);
            }
        }
        public async Task<PagedResponse<IEnumerable<EnrollmentResponse>>> GetEnrolledCoursesAsync(Guid userId, string lang, PagingRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
