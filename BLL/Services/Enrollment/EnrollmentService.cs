using BLL.IServices.Enrollment;
using BLL.IServices.Purchases;
using Common.DTO.ApiResponse;
using Common.DTO.Enrollment.Request;
using Common.DTO.Enrollment.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Enrollment
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPurchaseService _purchaseService;
        public EnrollmentService(IUnitOfWork unitOfWork, IPurchaseService purchaseService)
        {
            _unitOfWork = unitOfWork;
            _purchaseService = purchaseService;
        }
        public async Task<BaseResponse<EnrollmentResponse>> EnrolCourseAsync(Guid userId, EnrollmentRequest request)
        {
            var strategy = _unitOfWork.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId);
                    if (user == null)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Unauthorized", 401);

                    if (!user.Status)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Account is inactive", 403);

                    if (!user.IsEmailConfirmed)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Email not confirmed", 403);

                    var course = await _unitOfWork.Courses.Query()
                                    .Include(c => c.Teacher)
                                    .FirstOrDefaultAsync(c => c.CourseID == request.CourseId);
                    if (course == null)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Course not found", 404);

                    if (course.Teacher != null && course.Teacher.UserId == userId)
                    {
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "You cannot enroll in your own course.", 400);
                    }

                    if (course.Status != CourseStatus.Published)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "This course is not yet published and cannot be enrolled.", 400);

                    if (course.CourseType == CourseType.Free)
                    {
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "This enrollment API is only available for paid courses", 403);
                    }

                    var activeLearner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId && l.LanguageId == course.LanguageId);
                    if (activeLearner == null) return BaseResponse<EnrollmentResponse>.Fail(new object(), "Language profile not found", 400);

                    var validPurchase = await _unitOfWork.Purchases.Query()
                         .OrderByDescending(p => p.CreatedAt)
                         .FirstOrDefaultAsync(p => p.UserId == userId &&
                                              p.CourseId == request.CourseId &&
                                              p.Status == PurchaseStatus.Completed);

                    if (validPurchase == null)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Valid purchase not found", 403);

                    var existingEnrollment = await _unitOfWork.Enrollments.FindAsync(e => e.CourseId == request.CourseId && e.LearnerId == activeLearner.LearnerLanguageId);

                    DAL.Models.Enrollment enrollmentToReturn;

                    if (existingEnrollment != null)
                    {
                        if (existingEnrollment.Status == DAL.Type.EnrollmentStatus.Active)
                        {
                            return BaseResponse<EnrollmentResponse>.Fail(new object(), "You are already enrolled.", 400);
                        }

                        if (existingEnrollment.Status == DAL.Type.EnrollmentStatus.Expired ||
                            existingEnrollment.Status == DAL.Type.EnrollmentStatus.Cancelled)
                        {
                            existingEnrollment.Status = DAL.Type.EnrollmentStatus.Active;
                            existingEnrollment.EnrolledAt = TimeHelper.GetVietnamTime();
                            existingEnrollment.LastAccessedAt = TimeHelper.GetVietnamTime();

                            await _unitOfWork.Enrollments.UpdateAsync(existingEnrollment);
                            enrollmentToReturn = existingEnrollment;

                            course.LearnerCount++;
                        }
                        else
                        {
                            enrollmentToReturn = existingEnrollment;
                        }
                    }
                    else
                    {
                        var newEnrollment = new DAL.Models.Enrollment
                        {
                            EnrollmentID = Guid.NewGuid(),
                            CourseId = request.CourseId,
                            LearnerId = activeLearner.LearnerLanguageId,
                            TotalUnits = course.NumUnits,
                            TotalLessons = course.NumLessons,
                            Status = DAL.Type.EnrollmentStatus.Active,
                            EnrolledAt = TimeHelper.GetVietnamTime(),
                            ProgressPercent = 0
                        };
                        await _unitOfWork.Enrollments.CreateAsync(newEnrollment);
                        enrollmentToReturn = newEnrollment;

                        course.LearnerCount++;
                    }

                    validPurchase.EnrollmentId = enrollmentToReturn.EnrollmentID;
                    await _unitOfWork.Purchases.UpdateAsync(validPurchase);

                    await _unitOfWork.Courses.UpdateAsync(course);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    var accessResponse = await _purchaseService.CheckCourseAccessAsync(userId, course.CourseID);

                    var enrollmentResponse = new EnrollmentResponse
                    {
                        EnrollmentId = enrollmentToReturn.EnrollmentID,
                        CourseId = course.CourseID,
                        CourseType = course.CourseType.ToString(),
                        AccessUntil = accessResponse?.Data?.ExpiresAt,
                        EligibleForRefundUntil = accessResponse?.Data?.RefundEligibleUntil,
                        CourseTitle = course.Title,
                        PricePaid = validPurchase?.FinalAmount ?? 0,
                        Status = enrollmentToReturn.Status.ToString(),
                        ProgressPercent = enrollmentToReturn.ProgressPercent,
                        EnrollmentDate = enrollmentToReturn.EnrolledAt.ToString("dd-MM-yyyy HH:mm"),
                    };

                    return BaseResponse<EnrollmentResponse>.Success(enrollmentResponse, "Enrolled successfully.");
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return BaseResponse<EnrollmentResponse>.Error($"Unexpected error occurred: {ex.Message}");
                }
            });
        }
        public async Task<BaseResponse<EnrollmentResponse>> EnrolFreeCourseAsync(Guid userId, EnrollmentRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Unauthorized", 401);

                if (!user.Status)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Account is inactive", 403);

                if (!user.IsEmailConfirmed)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Email not confirmed", 403);

                var course = await _unitOfWork.Courses.Query()
                            .Include(c => c.Teacher)
                            .FirstOrDefaultAsync(c => c.CourseID == request.CourseId);

                if (course == null)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Course not found", 404);

                if (course.Teacher != null && course.Teacher.UserId == userId)
                {
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "You cannot enroll in your own course.", 400);
                }

                if (course.Status != CourseStatus.Published)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "This course is not yet published and cannot be enrolled.", 400);

                if (course.CourseType != CourseType.Free)
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Not a free course", 400);

                var activeLearner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId && l.LanguageId == course.LanguageId);
                if (activeLearner == null) return BaseResponse<EnrollmentResponse>.Fail(new object(), "Entrance test required", 400);

                var existingEnrollment = await _unitOfWork.Enrollments.FindAsync(e => e.CourseId == request.CourseId && e.LearnerId == activeLearner.LearnerLanguageId);

                if (existingEnrollment != null)
                {
                    if (existingEnrollment.Status == DAL.Type.EnrollmentStatus.Cancelled)
                    {
                        existingEnrollment.Status = DAL.Type.EnrollmentStatus.Active;
                        existingEnrollment.EnrolledAt = TimeHelper.GetVietnamTime();
                        await _unitOfWork.Enrollments.UpdateAsync(existingEnrollment);

                        course.LearnerCount++;
                        await _unitOfWork.Courses.UpdateAsync(course);
                        await _unitOfWork.SaveChangesAsync();

                        return BaseResponse<EnrollmentResponse>.Success(new EnrollmentResponse
                        {
                            EnrollmentId = existingEnrollment.EnrollmentID,
                            Status = "Active",
                        }, "Re-enrolled successfully");
                    }
                    return BaseResponse<EnrollmentResponse>.Fail(new object(), "Already enrolled", 400);
                }

                var enrollment = new DAL.Models.Enrollment
                {
                    EnrollmentID = Guid.NewGuid(),
                    CourseId = request.CourseId,
                    LearnerId = activeLearner.LearnerLanguageId,
                    TotalUnits = course.NumUnits,
                    TotalLessons = course.NumLessons,
                    EnrolledAt = TimeHelper.GetVietnamTime(),
                    Status = DAL.Type.EnrollmentStatus.Active
                };

                await _unitOfWork.Enrollments.CreateAsync(enrollment);

                course.LearnerCount++;
                await _unitOfWork.Courses.UpdateAsync(course);

                await _unitOfWork.SaveChangesAsync();

                var enrollmentResponse = new EnrollmentResponse
                {
                    EnrollmentId = enrollment.EnrollmentID,
                    CourseId = course.CourseID,
                    CourseType = course.CourseType.ToString(),
                    CourseTitle = course.Title,
                    Status = enrollment.Status.ToString(),
                    ProgressPercent = enrollment.ProgressPercent,
                    EnrollmentDate = enrollment.EnrolledAt.ToString("dd-MM-yyyy HH:mm"),
                };

                return BaseResponse<EnrollmentResponse>.Success(enrollmentResponse, "Enrolled successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<EnrollmentResponse>.Error($"Unexpected error occurred: {ex.Message}");
            }
        }
        public async Task<PagedResponse<IEnumerable<EnrollmentResponse>>> GetEnrolledCoursesAsync(Guid userId, string lang, PagingRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return PagedResponse<IEnumerable<EnrollmentResponse>>.Fail(new object(), "User not found", 404);

                if (!user.IsEmailConfirmed)
                    return PagedResponse<IEnumerable<EnrollmentResponse>>.Fail(new object(), "Please confirm your email to access this feature", 403);

                if (!user.Status)
                    return PagedResponse<IEnumerable<EnrollmentResponse>>.Fail(new object(), "Account is deactivated", 403);

                var language = await _unitOfWork.Languages.FindByLanguageCodeAsync(lang);
                if (language == null)
                    return PagedResponse<IEnumerable<EnrollmentResponse>>.Fail(new object(), "Language not found", 404);

                var activeLearner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId && l.LanguageId == language.LanguageID);

                if (activeLearner == null)
                    return PagedResponse<IEnumerable<EnrollmentResponse>>.Fail(new object(), "Please complete the entrance test for this language first", 404);

                var query = _unitOfWork.Enrollments.Query()
                    .Where(e => e.LearnerId == activeLearner.LearnerLanguageId)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Template)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Language)
                    .Include(e => e.Purchases)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(e =>
                        e.Course.Title.Contains(request.SearchTerm) ||
                        e.Course.Description.Contains(request.SearchTerm));
                }

                if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<DAL.Type.EnrollmentStatus>(request.Status, out var statusFilter))
                {
                    query = query.Where(e => e.Status == statusFilter);
                }

                var totalCount = await query.CountAsync();

                query = request.SortBy?.ToLower() switch
                {
                    "title" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.Course.Title)
                        : query.OrderBy(e => e.Course.Title),
                    "progress" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.ProgressPercent)
                        : query.OrderBy(e => e.ProgressPercent),
                    "enrolledat" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.EnrolledAt)
                        : query.OrderBy(e => e.EnrolledAt),
                    "lastaccessed" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.LastAccessedAt)
                        : query.OrderBy(e => e.LastAccessedAt),
                    _ => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.EnrolledAt)
                        : query.OrderBy(e => e.EnrolledAt)
                };

                var enrollments = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var enrollmentResponses = new List<EnrollmentResponse>();

                foreach (var enrollment in enrollments)
                {
                    var purchase = enrollment.Purchases?
                        .FirstOrDefault(p => p.Status == PurchaseStatus.Completed)
                        ?? await _unitOfWork.Purchases
                            .Query()
                            .FirstOrDefaultAsync(p => p.UserId == userId &&
                                                    p.CourseId == enrollment.CourseId &&
                                                    p.Status == PurchaseStatus.Completed);

                    if (purchase == null)
                    {
                        purchase = await _unitOfWork.Purchases
                            .Query()
                            .FirstOrDefaultAsync(p =>
                                p.UserId == userId &&
                                p.CourseId == enrollment.CourseId &&
                                p.Status == PurchaseStatus.Completed);
                    }

                    var courseAccess = await _purchaseService.CheckCourseAccessAsync(userId, enrollment.CourseId);
                    var response = new EnrollmentResponse
                    {
                        EnrollmentId = enrollment.EnrollmentID,
                        CourseId = enrollment.CourseId,
                        CourseTitle = enrollment.Course?.Title,
                        CourseType = enrollment.Course?.CourseType.ToString(),
                        PricePaid = purchase?.FinalAmount ?? 0,
                        ProgressPercent = enrollment.ProgressPercent,
                        EnrollmentDate = enrollment.EnrolledAt.ToString("dd-MM-yyyy HH:mm"),
                        AccessUntil = purchase?.ExpiresAt?.ToString("dd-MM-yyyy HH:mm"),
                        EligibleForRefundUntil = purchase?.EligibleForRefundUntil?.ToString("dd-MM-yyyy HH:mm"),
                        LastAccessedAt = enrollment.LastAccessedAt?.ToString("dd-MM-yyyy HH:mm"),
                        Status = enrollment.Status.ToString()
                    };

                    enrollmentResponses.Add(response);
                }

                return PagedResponse<IEnumerable<EnrollmentResponse>>.Success(
                    enrollmentResponses,
                    request.Page,
                    request.PageSize,
                    totalCount,
                    "Enrolled courses retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<EnrollmentResponse>>.Error(
                    $"Unexpected error occurred: {ex.Message}"
                );
            }
        }
        public async Task<PagedResponse<IEnumerable<EnrolledCourseOverviewResponse>>> GetEnrolledCoursesOverviewAsync(Guid userId, PagingRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null || !user.IsEmailConfirmed || !user.Status)
                    return PagedResponse<IEnumerable<EnrolledCourseOverviewResponse>>.Fail(new object(), "Account not activated or inactive", 403);

                var activeLearner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId && l.LanguageId == user.ActiveLanguageId);
                if (activeLearner == null)
                    return PagedResponse<IEnumerable<EnrolledCourseOverviewResponse>>.Fail(new object(), "Active language not configured", 403);

                var query = _unitOfWork.Enrollments.Query()
                    .Where(e => e.LearnerId == activeLearner.LearnerLanguageId)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Teacher)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Language)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Level)
                    .Include(e => e.UnitProgresses)
                    .AsNoTracking();

                if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<DAL.Type.EnrollmentStatus>(request.Status, out var statusFilter))
                {
                    query = query.Where(e => e.Status == statusFilter);
                }

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(e => e.Course.Title.Contains(request.SearchTerm) ||
                                           e.Course.Description.Contains(request.SearchTerm));
                }

                var totalCount = await query.CountAsync();

                query = request.SortBy?.ToLower() switch
                {
                    "title" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.Course.Title)
                        : query.OrderBy(e => e.Course.Title),
                    "progress" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.ProgressPercent)
                        : query.OrderBy(e => e.ProgressPercent),
                    "lastaccessed" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.LastAccessedAt)
                        : query.OrderBy(e => e.LastAccessedAt),
                    _ => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.LastAccessedAt ?? e.EnrolledAt)
                        : query.OrderBy(e => e.LastAccessedAt ?? e.EnrolledAt)
                };

                var enrollments = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var responses = new List<EnrolledCourseOverviewResponse>();

                foreach (var enrollment in enrollments)
                {
                    var course = enrollment.Course;
                    var teacher = course.Teacher;

                    var response = new EnrolledCourseOverviewResponse
                    {
                        EnrollmentId = enrollment.EnrollmentID,
                        CourseId = course.CourseID,
                        CourseTitle = course.Title,
                        CourseImage = course.ImageUrl ?? string.Empty,
                        Language = course.Language?.LanguageName ?? string.Empty,
                        Level = course.Level?.Name ?? string.Empty,
                        TeacherName = teacher?.FullName ?? string.Empty,
                        TeacherAvatar = teacher?.Avatar ?? string.Empty,
                        ProgressPercent = enrollment.ProgressPercent,
                        Status = enrollment.Status.ToString(),
                        LastAccessedAt = enrollment.LastAccessedAt?.ToString("dd-MM-yyyy HH:mm"),
                        EnrolledAt = enrollment.EnrolledAt.ToString("dd-MM-yyyy HH:mm"),
                        TotalLessons = enrollment.TotalLessons,
                        CompletedLessons = enrollment.CompletedLessons,
                        TotalUnits = enrollment.TotalUnits,
                        CompletedUnits = enrollment.CompletedUnits,
                        CurrentUnit = await GetCurrentUnitName(enrollment.CurrentUnitId),
                        CurrentLesson = await GetCurrentLessonName(enrollment.CurrentLessonId),
                        NextLesson = await GetNextLessonName(enrollment),
                        IsExpired = enrollment.Status == DAL.Type.EnrollmentStatus.Expired,
                        AccessUntil = await GetAccessUntil(enrollment.EnrollmentID)
                    };
                    responses.Add(response);
                }
                return PagedResponse<IEnumerable<EnrolledCourseOverviewResponse>>.Success(
                    responses, request.Page, request.PageSize, totalCount, "Enrolled courses retrieved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while fetching enrollment: {ex.Message}");
                return PagedResponse<IEnumerable<EnrolledCourseOverviewResponse>>.Error($"Error retrieving enrolled courses: {ex.Message}");
            }
        }
        public async Task<BaseResponse<EnrolledCourseDetailResponse>> GetEnrolledCourseDetailAsync(Guid userId, Guid enrollmentId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<EnrolledCourseDetailResponse>.Fail(new object(), "User not found", 404);

                if (!user.IsEmailConfirmed)
                    return BaseResponse<EnrolledCourseDetailResponse>.Fail(new object(), "Please confirm your email to access course content", 403);

                if (!user.Status)
                    return BaseResponse<EnrolledCourseDetailResponse>.Fail(new object(), "Account is deactivated", 403);

                var enrollment = await _unitOfWork.Enrollments.Query()
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Teacher)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Language)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Level)
                    .Include(e => e.UnitProgresses)
                    .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

                if (enrollment == null)
                    return BaseResponse<EnrolledCourseDetailResponse>.Fail(new object(), "Enrollment not found or you don't have access", 404);

                var course = enrollment.Course;
                var teacher = course.Teacher;

                var response = new EnrolledCourseDetailResponse
                {
                    EnrollmentId = enrollmentId,
                    Course = new CourseDetailDto
                    {
                        CourseId = course.CourseID,
                        Title = course.Title,
                        Description = course.Description,
                        Image = course.ImageUrl ?? string.Empty,
                        Language = course.Language?.LanguageName ?? string.Empty,
                        Level = course.Level.Name ?? string.Empty,
                        Duration = $"{course.DurationDays} days",
                        TotalUnits = enrollment.TotalUnits,
                        TotalLessons = enrollment.TotalLessons,
                        TotalExercises = await GetTotalExercises(course.CourseID),
                        Teacher = new TeacherInfoDto
                        {
                            TeacherId = teacher?.TeacherId ?? Guid.Empty,
                            Name = teacher?.FullName ?? string.Empty,
                            Avatar = teacher?.Avatar ?? string.Empty,
                            Rating = teacher?.AverageRating ?? 0.0,
                            TotalStudents = await GetTeacherStudentCount(teacher?.TeacherId)
                        },
                        Objective = course.LearningOutcome,
                    },
                    Progress = new ProgressDetailDto
                    {
                        OverallPercent = enrollment.ProgressPercent,
                        TotalTimeSpent = FormatTotalTime(enrollment.TotalTimeSpent),
                        LastAccessed = enrollment.LastAccessedAt?.ToString("dd-MM-yyyy HH:mm"),
                        CompletedUnits = enrollment.CompletedUnits,
                        CompletedLessons = enrollment.CompletedLessons,
                        CurrentUnit = await GetCurrentUnitDetail(enrollment.CurrentUnitId),
                        CurrentLesson = await GetCurrentLessonDetail(enrollment.CurrentLessonId),
                        UpcomingLesson = await GetUpcomingLesson(enrollment)
                    },
                    RecentActivities = await GetRecentActivities(enrollmentId)
                };

                return BaseResponse<EnrolledCourseDetailResponse>.Success(response, "Course detail retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<EnrolledCourseDetailResponse>.Error($"Error retrieving course detail: {ex.Message}");
            }
        }
        public async Task<BaseResponse<EnrolledCourseCurriculumResponse>> GetEnrolledCourseCurriculumAsync(Guid userId, Guid enrollmentId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<EnrolledCourseCurriculumResponse>.Fail(new object(), "User not found", 404);

                if (!user.IsEmailConfirmed)
                    return BaseResponse<EnrolledCourseCurriculumResponse>.Fail(new object(), "Please confirm your email to access curriculum", 403);

                if (!user.Status)
                    return BaseResponse<EnrolledCourseCurriculumResponse>.Fail(new object(), "Account is deactivated", 403);

                var enrollment = await _unitOfWork.Enrollments.Query()
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

                if (enrollment == null)
                    return BaseResponse<EnrolledCourseCurriculumResponse>.Fail(new object(), "Enrollment not found", 404);

                if (enrollment.Status == DAL.Type.EnrollmentStatus.Cancelled || enrollment.Status == DAL.Type.EnrollmentStatus.Expired)
                    return BaseResponse<EnrolledCourseCurriculumResponse>.Fail(
                        new object(),
                        "Enrollment has been cancelled or expired. Access to course curriculum is denied.",
                        400
                    );

                var units = await _unitOfWork.CourseUnits.Query()
                    .Where(u => u.CourseID == enrollment.CourseId)
                    .OrderBy(u => u.Position)
                    .Include(u => u.Lessons)
                        .ThenInclude(l => l.Exercises)
                    .ToListAsync();

                var unitProgresses = await _unitOfWork.UnitProgresses.Query()
                    .Where(up => up.EnrollmentId == enrollmentId)
                    .ToListAsync();

                var lessonProgresses = await _unitOfWork.LessonProgresses.Query()
                    .Where(lp => lp.UnitProgress != null && lp.UnitProgress.EnrollmentId == enrollmentId)
                    .Include(lp => lp.ExerciseSubmissions)
                    .ToListAsync();

                var curriculumUnits = new List<CurriculumUnitDto>();

                foreach (var unit in units)
                {
                    var unitProgress = unitProgresses.FirstOrDefault(up => up.CourseUnitId == unit.CourseUnitID);
                    var unitLessons = unit.Lessons.OrderBy(l => l.Position).ToList();

                    var curriculumLessons = new List<CurriculumLessonDto>();

                    foreach (var lesson in unitLessons)
                    {
                        var lessonProgress = lessonProgresses.FirstOrDefault(lp => lp.LessonId == lesson.LessonID);
                        var exerciseSubmissions = lessonProgress?.ExerciseSubmissions ?? new List<ExerciseSubmission>();
                        var hasPassedExercise = exerciseSubmissions.Any(es => es.IsPassed == true);

                        curriculumLessons.Add(new CurriculumLessonDto
                        {
                            LessonId = lesson.LessonID,
                            Title = lesson.Title,
                            Order = lesson.Position,
                            ProgressPercent = lessonProgress?.ProgressPercent ?? 0,
                            Status = lessonProgress?.Status.ToString() ?? "NotStarted",
                            HasContent = !string.IsNullOrEmpty(lesson.Content),
                            HasVideo = !string.IsNullOrEmpty(lesson.VideoUrl),
                            HasDocument = !string.IsNullOrEmpty(lesson.DocumentUrl),
                            HasExercise = lesson.Exercises.Any()
                        });
                    }

                    curriculumUnits.Add(new CurriculumUnitDto
                    {
                        UnitId = unit.CourseUnitID,
                        Title = unit.Title,
                        Order = unit.Position,
                        ProgressPercent = unitProgress?.ProgressPercent ?? 0,
                        Status = unitProgress?.Status.ToString() ?? "NotStarted",
                        CompletedAt = unitProgress?.CompletedAt?.ToString("dd-MM-yyyy HH:mm"),
                        Lessons = curriculumLessons
                    });
                }

                var response = new EnrolledCourseCurriculumResponse
                {
                    EnrollmentId = enrollmentId,
                    CourseTitle = enrollment.Course.Title,
                    Units = curriculumUnits
                };

                return BaseResponse<EnrolledCourseCurriculumResponse>.Success(response, "Curriculum retrieved successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<EnrolledCourseCurriculumResponse>.Error($"Error retrieving curriculum: {ex.Message}");
            }
        }
        public async Task<BaseResponse<List<ContinueLearningResponse>>> GetContinueLearningAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null)
                {
                    return BaseResponse<List<ContinueLearningResponse>>.Fail(
                        new List<ContinueLearningResponse>(),
                        "Access denied. Invalid authentication.",
                        401
                    );
                }

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId && l.LanguageId == user.ActiveLanguageId);

                if (learner == null)
                    return BaseResponse<List<ContinueLearningResponse>>.Fail(new List<ContinueLearningResponse>(), "Learner not found", 403);

                var enrollments = await _unitOfWork.Enrollments.Query()
                    .Where(e => e.LearnerId == learner.LearnerLanguageId &&
                               e.Status == DAL.Type.EnrollmentStatus.Active &&
                               e.ProgressPercent < 100 && e.Course.LanguageId == learner.LanguageId)
                    .Include(e => e.Course)
                    .OrderByDescending(e => e.LastAccessedAt ?? e.EnrolledAt)
                    .Take(5)
                    .ToListAsync();

                var responses = new List<ContinueLearningResponse>();

                foreach (var enrollment in enrollments)
                {
                    var continueLesson = await GetContinueLesson(enrollment);
                    var lastAccessed = FormatTimeAgo(enrollment.LastAccessedAt ?? enrollment.EnrolledAt);

                    responses.Add(new ContinueLearningResponse
                    {
                        EnrollmentId = enrollment.EnrollmentID,
                        CourseId = enrollment.CourseId,
                        CourseTitle = enrollment.Course.Title,
                        CourseImage = enrollment.Course.ImageUrl ?? string.Empty,
                        ProgressPercent = enrollment.ProgressPercent,
                        ContinueLesson = continueLesson,
                        LastAccessed = lastAccessed
                    });
                }

                return BaseResponse<List<ContinueLearningResponse>>.Success(responses, "Continue learning courses retrieved");
            }
            catch (Exception ex)
            {
                return BaseResponse<List<ContinueLearningResponse>>.Error($"Error retrieving continue learning: {ex.Message}");
            }
        }
        public async Task<BaseResponse<bool>> ResumeCourseAsync(Guid userId, Guid enrollmentId, ResumeCourseRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponse<bool>.Fail(
                        new object(),
                        "Access denied. Invalid authentication.",
                        401
                    );
                }

                var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId);
                if (enrollment == null)
                    return BaseResponse<bool>.Fail(false, "Enrollment not found", 404);

                enrollment.CurrentUnitId = request.UnitId;
                enrollment.CurrentLessonId = request.LessonId;
                enrollment.LastAccessedAt = TimeHelper.GetVietnamTime();

                await _unitOfWork.Enrollments.UpdateAsync(enrollment);
                await _unitOfWork.SaveChangesAsync();

                return BaseResponse<bool>.Success(true, "Course resumed successfully");
            }
            catch (Exception ex)
            {
                return BaseResponse<bool>.Error($"Error resuming course: {ex.Message}");
            }
        }
        #region
        private string FormatTotalTime(int totalMinutes)
        {
            if (totalMinutes < 60) return $"{totalMinutes} mins";
            var hours = totalMinutes / 60;
            var mins = totalMinutes % 60;
            return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
        }
        private async Task<CurrentUnitDto?> GetCurrentUnitDetail(Guid? unitId)
        {
            if (!unitId.HasValue) return null;
            var unit = await _unitOfWork.CourseUnits.GetByIdAsync(unitId.Value);
            if (unit == null) return null;

            return new CurrentUnitDto
            {
                UnitId = unit.CourseUnitID,
                Title = unit.Title,
                ProgressPercent = 0
            };
        }
        private async Task<CurrentLessonDto?> GetCurrentLessonDetail(Guid? lessonId)
        {
            if (!lessonId.HasValue) return null;
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId.Value);
            if (lesson == null) return null;

            return new CurrentLessonDto
            {
                LessonId = lesson.LessonID,
                Title = lesson.Title,
                ProgressPercent = 0 // Would need to calculate from lesson progress
            };
        }
        private async Task<UpcomingLessonDto?> GetUpcomingLesson(DAL.Models.Enrollment enrollment)
        {
            if (!enrollment.CurrentLessonId.HasValue) return null;

            var currentLesson = await _unitOfWork.Lessons.GetByIdAsync(enrollment.CurrentLessonId.Value);
            if (currentLesson == null) return null;

            var nextLesson = await _unitOfWork.Lessons
                .Query()
                .Where(l => l.CourseUnitID == currentLesson.CourseUnitID && l.Position > currentLesson.Position)
                .OrderBy(l => l.Position)
                .FirstOrDefaultAsync();

            if (nextLesson == null) return null;

            return new UpcomingLessonDto
            {
                LessonId = nextLesson.LessonID,
                Title = nextLesson.Title
            };
        }
        private async Task<List<RecentActivityDto>> GetRecentActivities(Guid enrollmentId)
        {
            var recentLogs = await _unitOfWork.LessonActivityLogs.Query()
                .Where(lal => lal.LessonProgress.UnitProgress.EnrollmentId == enrollmentId)
                .OrderByDescending(lal => lal.CreatedAt)
                .Take(5)
                .Include(lal => lal.Lesson)
                .ToListAsync();

            return recentLogs.Select(log => new RecentActivityDto
            {
                Type = log.ActivityType.ToString(),
                Title = $"{log.ActivityType}: {log.Lesson.Title}",
                Time = FormatTimeAgo(log.CreatedAt)
            }).ToList();
        }
        private string FormatTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} mins ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} days ago";
            return dateTime.ToString("MMM dd, yyyy");
        }
        private async Task<int> GetTeacherStudentCount(Guid? teacherId)
        {
            if (!teacherId.HasValue) return 0;
            return await _unitOfWork.Enrollments
                .Query()
                .CountAsync(e => e.Course.TeacherId == teacherId.Value);
        }
        private async Task<int> GetTotalExercises(Guid courseId)
        {
            return await _unitOfWork.Exercises.Query().CountAsync(e => e.Lesson.CourseUnit.CourseID == courseId);
        }
        private async Task<string> GetCurrentUnitName(Guid? unitId)
        {
            if (!unitId.HasValue) return null;
            var unit = await _unitOfWork.CourseUnits.GetByIdAsync(unitId.Value);
            return unit?.Title ?? "Untitled unit";
        }
        private async Task<string> GetCurrentLessonName(Guid? lessonId)
        {
            if (!lessonId.HasValue) return null;
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(lessonId.Value);
            return lesson?.Title ?? "Untitled lesson";
        }
        private async Task<string> GetNextLessonName(DAL.Models.Enrollment enrollment)
        {
            if (!enrollment.CurrentLessonId.HasValue) return null;

            var currentLesson = await _unitOfWork.Lessons.GetByIdAsync(enrollment.CurrentLessonId.Value);
            if (currentLesson == null) return null;

            var nextLesson = await _unitOfWork.Lessons
                .Query()
                .Where(l => l.CourseUnitID == currentLesson.CourseUnitID && l.Position > currentLesson.Position)
                .OrderBy(l => l.Position)
                .FirstOrDefaultAsync();

            return nextLesson?.Title ?? "Untitled lesson";
        }
        private async Task<string?> GetAccessUntil(Guid enrollmentId)
        {
            var purchase = await _unitOfWork.Purchases.Query()
                .FirstOrDefaultAsync(p => p.EnrollmentId == enrollmentId && p.Status == PurchaseStatus.Completed);
            return purchase?.ExpiresAt?.ToString("dd-MM-yyyy");
        }
        private async Task<ContinueLessonDto?> GetContinueLesson(DAL.Models.Enrollment enrollment)
        {
            if (!enrollment.CurrentLessonId.HasValue) return null;

            var lesson = await _unitOfWork.Lessons.GetByIdAsync(enrollment.CurrentLessonId.Value);
            if (lesson == null) return null;

            var lessonProgress = await _unitOfWork.LessonProgresses.FindAsync(lp =>
                lp.LessonId == enrollment.CurrentLessonId &&
                lp.UnitProgress != null &&
                lp.UnitProgress.EnrollmentId == enrollment.EnrollmentID);

            return new ContinueLessonDto
            {
                LessonId = lesson.LessonID,
                Title = lesson.Title,
                ProgressPercent = lessonProgress?.ProgressPercent ?? 0
            };
        }
        #endregion
    }
}
