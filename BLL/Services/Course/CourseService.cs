using BLL.IServices.Course;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Course.Request;
using Common.DTO.Course.Response;
using Common.DTO.CourseUnit.Response;
using Common.DTO.Lesson.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Topic.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Course
{
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _unit;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<CourseService> _logger;
        public CourseService(IUnitOfWork unit, ICloudinaryService cloudinaryService, ILogger<CourseService> logger)
        {
            _unit = unit;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        public Task<BaseResponse<object>> ApproveCourseSubmissionAsync(Guid userId, Guid submissionId)
        {
            throw new NotImplementedException();
        }

        //public async Task<BaseResponse<object>> ApproveCourseSubmissionAsync(Guid userId, Guid submissionId)
        //{
        //    try
        //    {
        //        var staff = await _unit.StaffLanguages.FindAsync(x => x.UserId == userId);
        //        if (staff == null)
        //            return BaseResponse<object>.Fail(null, "Staff does not exist.", 404);


        //        var submission = await _unit.CourseSubmissions.Query()
        //            .Include(cs => cs.Course)
        //            .FirstOrDefaultAsync(cs => cs.CourseSubmissionID == submissionId);

        //        if (submission == null)
        //            return BaseResponse<object>.Fail(null, "Course submission does not exist.", 404);

        //        if (staff.LanguageId != submission.Course.LanguageId)
        //            return BaseResponse<object>.Fail(null, "You are not authorized to approve submissions in this language.", 403);

        //        if (submission.Status != SubmissionStatus.Pending)
        //            return BaseResponse<object>.Fail(null, "This submission has already been reviewed and cannot be approved again.", 400);

        //        submission.Status = SubmissionStatus.Approved;
        //        submission.ReviewedById = staff.StaffLanguageId;
        //        submission.ReviewedAt = TimeHelper.GetVietnamTime();

        //        submission.Course.Status = CourseStatus.Published;
        //        submission.Course.ApprovedByID = staff.StaffLanguageId;
        //        submission.Course.PublishedAt = TimeHelper.GetVietnamTime();
        //        submission.Course.UpdatedAt = TimeHelper.GetVietnamTime();

        //        await _unit.CourseSubmissions.UpdateAsync(submission);
        //        await _unit.Courses.UpdateAsync(submission.Course);
        //        await _unit.SaveChangesAsync();

        //        return BaseResponse<object>.Success(new { submissionId = submission.CourseSubmissionID }, "Course submission approved successfully.");

        //    }
        //    catch (Exception ex)
        //    {
        //        return BaseResponse<object>.Fail(null, $"An unexpected error occurred: {ex.Message}", 500);
        //    }
        //}

        public async Task<BaseResponse<CourseResponse>> CreateCourseAsync(Guid userId, CourseRequest request)
        {
            return await _unit.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);

                    if (teacher == null)
                        return BaseResponse<CourseResponse>.Fail(new object(), "Teacher does not exist.", 403);

                    var template = await _unit.CourseTemplates.GetByIdAsync(request.TemplateId);
                    if (template == null)
                        return BaseResponse<CourseResponse>.Fail("Template does not exist");

                    if (!template.Status)
                        return BaseResponse<CourseResponse>.Fail("Template is not active");

                    var language = await _unit.Languages.GetByIdAsync(teacher.LanguageId);
                    var program = await _unit.Programs.GetByIdAsync(template.ProgramId);
                    var level = await _unit.Levels.GetByIdAsync(template.LevelId);

                    if (program == null || level == null)
                        return BaseResponse<CourseResponse>.Fail("Invalid language, program or level");

                    var topicIds = new List<Guid>();

                    if (!string.IsNullOrWhiteSpace(request.TopicIds))
                    {
                        topicIds = request.TopicIds
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => Guid.TryParse(x.Trim(), out var guid) ? guid : Guid.Empty)
                            .Where(g => g != Guid.Empty)
                            .ToList();
                    }

                    var validationErrors = new Dictionary<string, string>();

                    var validTopics = new List<DAL.Models.Topic>();
                    if (topicIds.Any())
                    {
                        validTopics = (List<DAL.Models.Topic>)await _unit.Topics.FindAllAsync(
                            t => topicIds.Contains(t.TopicID));
                        var validIds = validTopics.Select(t => t.TopicID).ToHashSet();
                        var invalidIds = topicIds.Where(id => !validIds.Contains(id)).ToList();
                        if (invalidIds.Any())
                            return BaseResponse<CourseResponse>.Fail($"Invalid topic IDs: {string.Join(", ", invalidIds)}");
                    }

                    switch (request.CourseType)
                    {
                        case CourseType.Free:
                            if (request.Price != 0)
                                validationErrors[nameof(request.Price)] = "Price must be 0 for free courses.";
                            break;
                        case CourseType.Paid:
                            if (request.Price <= 0)
                                validationErrors[nameof(request.Price)] = "Price must be greater than 0 for paid courses.";
                            break;
                        default:
                            validationErrors[nameof(request.CourseType)] = "Invalid course type.";
                            break;
                    }

                    if (validationErrors.Any())
                    {
                        return BaseResponse<CourseResponse>.Fail(
                            validationErrors,
                            "Course request does not satisfy the selected course template requirements.",
                            400
                        );
                    }

                    string imageUrl = "";
                    string publicId = "";
                    if (request.Image != null)
                    {
                        var result = await _cloudinaryService.UploadImageAsync(request.Image);
                        if (result.Url == null)
                        {
                            return BaseResponse<CourseResponse>.Fail("Failed when uploading the image.");
                        }
                        imageUrl = result.Url;
                        publicId = result.PublicId;
                    }

                    var course = new DAL.Models.Course
                    {
                        CourseID = Guid.NewGuid(),
                        TemplateId = request.TemplateId,
                        LanguageId = language.LanguageID,
                        ProgramId = template.ProgramId,
                        LevelId = template.LevelId,
                        TeacherId = teacher.TeacherId,
                        Title = request.Title ?? "Untitled Course",
                        Description = request.Description ?? "No description",
                        LearningOutcome = request.LearningOutcome ?? "No learning outcomes",
                        ImageUrl = imageUrl,
                        PublicId = publicId,
                        Price = request.Price,
                        CourseType = request.CourseType,
                        GradingType = request.GradingType,
                        DurationDays = request.DurationDays,
                        NumUnits = template.UnitCount,
                        NumLessons = template.UnitCount * template.LessonsPerUnit,
                        EstimatedHours = CalculateEstimatedHours(template),
                        Status = CourseStatus.Draft,
                    };

                    await _unit.Courses.CreateAsync(course);

                    await AddCourseTopicsAsync(course.CourseID, validTopics);
                    await CreateCourseStructureAsync(course.CourseID, template);
                    await _unit.SaveChangesAsync();
                    await _unit.CommitTransactionAsync();

                    return await GetCourseByIdAsync(course.CourseID);
                }
                catch (Exception ex)
                {
                    return BaseResponse<CourseResponse>.Error(ex.Message);
                }
            });
        }
        #region Private Methods
        private async Task AddCourseTopicsAsync(Guid courseId, List<DAL.Models.Topic> topics)
        {
            foreach (var topic in topics)
            {
                var courseTopic = new CourseTopic
                {
                    CourseTopicID = Guid.NewGuid(),
                    CourseID = courseId,
                    TopicID = topic.TopicID,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };
                await _unit.CourseTopics.CreateAsync(courseTopic);
            }
        }
        private async Task CreateCourseStructureAsync(Guid courseId, DAL.Models.CourseTemplate template)
        {
            for (int unitPosition = 1; unitPosition <= template.UnitCount; unitPosition++)
            {
                var courseUnit = new CourseUnit
                {
                    CourseUnitID = Guid.NewGuid(),
                    CourseID = courseId,
                    Title = $"Unit {unitPosition}",
                    Description = $"Unit {unitPosition} description",
                    Position = unitPosition,
                    TotalLessons = template.LessonsPerUnit,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };

                await _unit.CourseUnits.CreateAsync(courseUnit);

                for (int lessonPosition = 1; lessonPosition <= template.LessonsPerUnit; lessonPosition++)
                {
                    var lesson = new Lesson
                    {
                        LessonID = Guid.NewGuid(),
                        CourseUnitID = courseUnit.CourseUnitID,
                        Content = $@"
                        <h1>Lesson {unitPosition}.{lessonPosition}: Introduction</h1>
                        <p>Welcome to this lesson! In this session, we will cover:</p>
                        <ul>
                        <li>Key concepts and terminology</li>
                        <li>Practical examples and exercises</li>
                        <li>Best practices and tips</li>
                        </ul>
                        <p>By the end of this lesson, you will be able to understand and apply the core concepts discussed.</p>
                        <h2>Learning Objectives</h2>
                        <p>After completing this lesson, you should be able to:</p>
                        <ol>
                        <li>Understand the main concepts</li>
                        <li>Apply the knowledge in practical scenarios</li>
                        <li>Complete the assigned exercises</li>
                        </ol>
                        ",
                        Title = $"Lesson {unitPosition}.{lessonPosition}",
                        Description = $"Lesson {unitPosition}.{lessonPosition} description",
                        Position = lessonPosition,
                        VideoUrl = "",
                        VideoPublicId = "",
                        DocumentUrl = "",
                        DocumentPublicId = "",
                        TotalExercises = template.ExercisesPerLesson,
                        CreatedAt = TimeHelper.GetVietnamTime(),
                        UpdatedAt = TimeHelper.GetVietnamTime()
                    };

                    await _unit.Lessons.CreateAsync(lesson);
                }
            }
        }
        private int CalculateEstimatedHours(DAL.Models.CourseTemplate template)
        {
            // Simple calculation: 1 unit = 2 hours, 1 lesson = 30 minutes
            return template.UnitCount * 2 + template.UnitCount * template.LessonsPerUnit / 2;
        }
        #endregion
        public async Task<bool> DeleteCourseAsync(Guid courseId)
        {
            var course = await _unit.Courses.GetByIdAsync(courseId);
            if (course == null)
                return false;

            if (course.Status == CourseStatus.Published)
                throw new InvalidOperationException("Cannot delete published course");

            await _unit.Courses.DeleteAsync(course.CourseID);
            await _unit.SaveChangesAsync();
            return true;
        }

        public async Task<BaseResponse<CourseResponse>> GetCourseByIdAsync(Guid courseId)
        {
            var course = await _unit.Courses.GetByIdAsync(courseId);
            if (course == null)
                return BaseResponse<CourseResponse>.Fail("Course not found");

            var language = await _unit.Languages.GetByIdAsync(course.LanguageId);
            var program = await _unit.Programs.GetByIdAsync(course.ProgramId);
            var level = await _unit.Levels.GetByIdAsync(course.LevelId);
            var teacher = await _unit.TeacherProfiles.GetByIdAsync(course.TeacherId);

            var courseTopics = await _unit.CourseTopics.GetTopicsByCourseAsync(courseId);
            var topics = new List<TopicResponse>();

            foreach (var ct in courseTopics)
            {
                var topic = await _unit.Topics.GetByIdAsync(ct.TopicID);
                if (topic != null)
                    topics.Add(new TopicResponse
                    {
                        TopicId = topic.TopicID,
                        TopicName = topic.Name ?? "Unknown name",
                        TopicDescription = topic.Description ?? "No description",
                        ImageUrl = topic.ImageUrl ?? "No image",
                    });
            }

            var units = await _unit.CourseUnits.FindAllAsync(cu => cu.CourseID == courseId);
            var unitResponses = new List<UnitResponse>();

            foreach (var unit in units)
            {
                var lessons = await _unit.Lessons.FindAllAsync(l => l.CourseUnitID == unit.CourseUnitID);
                var lessonResponses = lessons.Select(l => new LessonResponse
                {
                    LessonID = l.LessonID,
                    Title = l.Title,
                    Description = l.Description,
                    Position = l.Position,
                    TotalExercises = l.TotalExercises
                }).ToList();

                unitResponses.Add(new UnitResponse
                {
                    CourseUnitID = unit.CourseUnitID,
                    Title = unit.Title,
                    Description = unit.Description,
                    Position = unit.Position,
                    TotalLessons = unit.TotalLessons ?? 0,
                    IsPreview = unit.IsPreview ?? false,
                    Lessons = lessonResponses
                });
            }

            var programResponse = new Common.DTO.Course.Response.Program
            {
                ProgramId = program.ProgramId,
                Name = program.Name,
                Description = program.Description,
                Level = new Common.DTO.Course.Response.Level
                {
                    LevelId = level.LevelId,
                    Description = level.Description,
                    Name = level.Name,
                }
            };

            var teacherResponse = new Common.DTO.Course.Response.Teacher
            {
                TeacherId = teacher.TeacherId,
                Avatar = teacher.Avatar,
                Email = teacher.Email,
                Name = teacher.FullName,
            };

            var response = new CourseResponse
            {
                CourseId = course.CourseID,
                TemplateId = course.TemplateId,
                Language = language.LanguageName,
                Program = programResponse,
                Teacher = teacherResponse,
                Title = course.Title,
                Description = course.Description,
                LearningOutcome = course.LearningOutcome,
                ImageUrl = course.ImageUrl,
                Price = course.Price,
                DiscountPrice = course.DiscountPrice,
                CourseType = course.CourseType.ToString(),
                GradingType = course.GradingType.ToString(),
                LearnerCount = course.LearnerCount,
                AverageRating = course.AverageRating,
                ReviewCount = course.ReviewCount,
                NumLessons = course.NumLessons,
                NumUnits = course.NumUnits,
                DurationDays = course.DurationDays,
                EstimatedHours = course.EstimatedHours,
                CourseStatus = course.Status.ToString(),
                PublishedAt = course.PublishedAt?.ToString("dd-MM-yyyy"),
                CreatedAt = course.CreatedAt.ToString("dd-MM-yyyy"),
                ModifiedAt = course.UpdatedAt.ToString("dd-MM-yyyy"),
                Topics = topics,
                Units = unitResponses
            };

            return BaseResponse<CourseResponse>.Success(response);
        }

        public Task<PagedResponse<IEnumerable<CourseResponse>>> GetCoursesAsync(PagingRequest request, string status, string lang)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<IEnumerable<CourseResponse>>> GetCoursesByTeacherAsync(Guid userId, PagingRequest request, string status)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetCourseSubmissionsByStaffAsync(Guid userId, PagingRequest request, string status)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetCourseSubmissionsByTeacherAsync(Guid userId, PagingRequest request, string status)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<object>> RejectCourseSubmissionAsync(Guid userId, Guid submissionId, string reason)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<object>> SubmitCourseForReviewAsync(Guid userId, Guid courseId)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<CourseResponse>> UpdateCourseAsync(Guid userId, Guid courseId, UpdateCourseRequest request)
        {
            throw new NotImplementedException();
        }

        //public async Task<PagedResponse<IEnumerable<CourseResponse>>> GetAllCoursesAsync(PagingRequest request, string status, string lang)
        //{
        //    try
        //    {
        //        if (request.Page <= 0) request.Page = 1;
        //        if (request.PageSize <= 0) request.PageSize = 10;

        //        var query = _unit.Courses.Query()
        //            .Include(c => c.Template)
        //            .Include(c => c.Teacher)
        //            .Include(c => c.Language)
        //            .Include(c => c.CourseGoals)
        //                .ThenInclude(cg => cg.Goal)
        //            .Include(c => c.CourseTopics)
        //                .ThenInclude(ct => ct.Topic)
        //            .AsQueryable();

        //        if (!string.IsNullOrWhiteSpace(status))
        //        {
        //            if (Enum.TryParse<CourseStatus>(status, true, out var parsedStatus))
        //            {
        //                query = query.Where(c => c.Status == parsedStatus);
        //            }
        //            else
        //            {
        //                return PagedResponse<IEnumerable<CourseResponse>>.Fail(
        //                    errors: null,
        //                    message: $"Invalid course status: '{status}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(CourseStatus)))}",
        //                    code: 400
        //                );
        //            }
        //        }

        //        if (!string.IsNullOrWhiteSpace(lang))
        //        {
        //            query = query.Where(c => c.Language.LanguageCode.ToLower() == lang.ToLower());
        //        }

        //        var totalCount = await query.CountAsync();

        //        var skip = (request.Page - 1) * request.PageSize;
        //        var courses = await query
        //            .OrderByDescending(c => c.CreatedAt)
        //            .Skip(skip)
        //            .Take(request.PageSize)
        //            .ToListAsync();

        //        // Mapping sang DTO
        //        var courseResponses = courses.Select(c => new CourseResponse
        //        {
        //            CourseID = c.CourseID,
        //            Title = c.Title,
        //            Description = c.Description,
        //            ImageUrl = c.ImageUrl,
        //            Price = c.Price,
        //            DiscountPrice = c.DiscountPrice,
        //            CourseType = c.Type.ToString(),
        //            CourseLevel = c.Level.ToString(),
        //            Status = c.Status.ToString(),
        //            CreatedAt = c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //            ModifiedAt = c.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //            NumLessons = c.NumLessons,

        //            TemplateInfo = c.Template != null
        //                ? new TemplateInfo
        //                {
        //                    TemplateId = c.Template.Id,
        //                    Name = c.Template.Name
        //                }
        //                : null,

        //            TeacherInfo = c.Teacher != null
        //                ? new TeacherInfo
        //                {
        //                    TeacherId = c.Teacher.TeacherProfileId,
        //                    FullName = c.Teacher.FullName ?? "Unknown",
        //                    Avatar = c.Teacher.Avatar ?? string.Empty,
        //                    Email = c.Teacher.Email ?? string.Empty,
        //                    PhoneNumber = c.Teacher.PhoneNumber ?? string.Empty
        //                }
        //                : null,

        //            LanguageInfo = c.Language != null
        //                ? new LanguageInfo
        //                {
        //                    Name = c.Language.LanguageName ?? "Unknown",
        //                    Code = c.Language.LanguageCode ?? "Unknown"
        //                }
        //                : null,

        //            Goals = c.CourseGoals != null && c.CourseGoals.Any()
        //                ? c.CourseGoals.Select(cg => new GoalResponse
        //                {
        //                    Id = cg.Goal.Id,
        //                    Description = cg.Goal.Description,
        //                    Name = cg.Goal.Name ?? "Unknown",
        //                }).ToList()
        //                : new List<GoalResponse>(),

        //            Topics = c.CourseTopics?
        //                .Select(ct => new TopicResponse
        //                {
        //                    TopicId = ct.Topic.TopicID,
        //                    TopicName = ct.Topic.Name,
        //                    TopicDescription = ct.Topic.Description ?? string.Empty,
        //                    ImageUrl = ct.Topic.ImageUrl ?? string.Empty
        //                })
        //                .ToList() ?? new List<TopicResponse>()
        //        }).ToList();

        //        return PagedResponse<IEnumerable<CourseResponse>>.Success(
        //            courseResponses,
        //            request.Page,
        //            request.PageSize,
        //            totalCount);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError(ex, "Error occurred while getting all courses.");
        //        return PagedResponse<IEnumerable<CourseResponse>>.Error(
        //            "An unexpected error occurred while fetching courses. Please try again later."
        //        );
        //    }
        //}

        //public async Task<PagedResponse<IEnumerable<CourseResponse>>> GetAllCoursesByTeacherIdAsync(Guid userId, PagingRequest request, string status)
        //{
        //    var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
        //    if (teacher == null)
        //        return PagedResponse<IEnumerable<CourseResponse>>.Fail(null, "Teacher does not exist.", 400);
        //    try
        //    {
        //        if (request.Page <= 0) request.Page = 1;
        //        if (request.PageSize <= 0) request.PageSize = 10;

        //        var query = _unit.Courses.Query()
        //            .Include(c => c.Template)
        //            .Include(c => c.Teacher)
        //            .Include(c => c.Language)
        //            .Include(c => c.CourseGoals)
        //                .ThenInclude(cg => cg.Goal)
        //            .Include(c => c.CourseTopics)
        //                .ThenInclude(ct => ct.Topic)
        //            .Where(c => c.TeacherId == teacher.TeacherProfileId)
        //            .AsQueryable();

        //        if (!string.IsNullOrWhiteSpace(status))
        //        {
        //            if (Enum.TryParse<CourseStatus>(status, true, out var parsedStatus))
        //            {
        //                query = query.Where(c => c.Status == parsedStatus);
        //            }
        //            else
        //            {
        //                return PagedResponse<IEnumerable<CourseResponse>>.Fail(null,
        //                    $"Invalid course status: '{status}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(CourseStatus)))}",
        //                    400
        //                );
        //            }
        //        }

        //        var totalCount = await query.CountAsync();

        //        var skip = (request.Page - 1) * request.PageSize;
        //        var courses = await query
        //            .OrderByDescending(c => c.CreatedAt)
        //            .Skip(skip)
        //            .Take(request.PageSize)
        //            .ToListAsync();

        //        // Mapping sang DTO
        //        var courseResponses = courses.Select(c => new CourseResponse
        //        {
        //            CourseID = c.CourseID,
        //            Title = c.Title,
        //            Description = c.Description,
        //            ImageUrl = c.ImageUrl,
        //            Price = c.Price,
        //            DiscountPrice = c.DiscountPrice,
        //            CourseType = c.Type.ToString(),
        //            CourseLevel = c.Level.ToString(),
        //            Status = c.Status.ToString(),
        //            CreatedAt = c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //            ModifiedAt = c.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //            NumLessons = c.NumLessons,

        //            TemplateInfo = c.Template != null
        //                ? new TemplateInfo
        //                {
        //                    TemplateId = c.Template.Id,
        //                    Name = c.Template.Name
        //                }
        //                : null,

        //            TeacherInfo = c.Teacher != null
        //                ? new TeacherInfo
        //                {
        //                    TeacherId = c.Teacher.TeacherProfileId,
        //                    FullName = c.Teacher.FullName ?? "Unknown",
        //                    Avatar = c.Teacher.Avatar ?? string.Empty,
        //                    Email = c.Teacher.Email ?? string.Empty,
        //                    PhoneNumber = c.Teacher.PhoneNumber ?? string.Empty
        //                }
        //                : null,

        //            LanguageInfo = c.Language != null
        //                ? new LanguageInfo
        //                {
        //                    Name = c.Language.LanguageName ?? "Unknown",
        //                    Code = c.Language.LanguageCode ?? "Unknown"
        //                }
        //                : null,

        //            Goals = c.CourseGoals != null && c.CourseGoals.Any()
        //                ? c.CourseGoals.Select(cg => new GoalResponse
        //                {
        //                    Id = cg.Goal.Id,
        //                    Description = cg.Goal.Description,
        //                    Name = cg.Goal.Name ?? "Unknown",
        //                }).ToList()
        //                : new List<GoalResponse>(),

        //            Topics = c.CourseTopics?
        //                .Select(ct => new TopicResponse
        //                {
        //                    TopicId = ct.Topic.TopicID,
        //                    TopicName = ct.Topic.Name,
        //                    TopicDescription = ct.Topic.Description ?? string.Empty,
        //                    ImageUrl = ct.Topic.ImageUrl ?? string.Empty
        //                })
        //                .ToList() ?? new List<TopicResponse>()
        //        }).ToList();

        //        return PagedResponse<IEnumerable<CourseResponse>>.Success(
        //            courseResponses,
        //            request.Page,
        //            request.PageSize,
        //            totalCount);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError(ex, "Error occurred while getting all courses.");
        //        return (PagedResponse<IEnumerable<CourseResponse>>)PagedResponse<IEnumerable<CourseResponse>>.Error(
        //            "An unexpected error occurred while fetching courses. Please try again later."
        //        );
        //    }
        //}

        //public async Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetAllCourseSubmissionsByStaffAsync(Guid userId, PagingRequest request, string status)
        //{
        //    try
        //    {
        //        var staff = await _unit.StaffLanguages.Query()
        //            .Include(s => s.User)
        //            .FirstOrDefaultAsync(x => x.UserId == userId);

        //        if (staff == null)
        //            return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Fail(null, "Staff does not exist.", 404);

        //        SubmissionStatus? filterStatus = null;
        //        if (!string.IsNullOrWhiteSpace(status))
        //        {
        //            if (Enum.TryParse<SubmissionStatus>(status, true, out var parsedStatus))
        //                filterStatus = parsedStatus;
        //            else
        //                return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Fail(null, "Invalid submission status.", 400);
        //        }

        //        var query = _unit.CourseSubmissions.Query()
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.Language)
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.Template)
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.CourseGoals)
        //                    .ThenInclude(cg => cg.Goal)
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.CourseTopics)
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.ApprovedBy)
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.Teacher)
        //                    .ThenInclude(t => t.User)
        //            .Where(cs => cs.Course.LanguageId == staff.LanguageId);

        //        if (filterStatus.HasValue)
        //            query = query.Where(cs => cs.Status == filterStatus.Value);

        //        var totalRecords = await query.CountAsync();

        //        var submissions = await query
        //            .OrderByDescending(cs => cs.SubmittedAt)
        //            .Skip((request.Page - 1) * request.PageSize)
        //            .Take(request.PageSize)
        //            .Select(cs => new CourseSubmissionResponse
        //            {
        //                CourseSubmissionID = cs.CourseSubmissionID,
        //                SubmissionStatus = cs.Status.ToString(),
        //                Feedback = cs.Feedback,
        //                SubmittedAt = cs.SubmittedAt,
        //                ReviewedAt = cs.ReviewedAt,
        //                Course = new CourseResponse
        //                {
        //                    CourseID = cs.Course.CourseID,
        //                    Title = cs.Course.Title,
        //                    Description = cs.Course.Description,
        //                    ImageUrl = cs.Course.ImageUrl,
        //                    Price = cs.Course.Price,
        //                    DiscountPrice = cs.Course.DiscountPrice,
        //                    CourseType = cs.Course.Type.ToString(),
        //                    CourseLevel = cs.Course.Level.ToString(),
        //                    Status = cs.Course.Status.ToString(),
        //                    CreatedAt = cs.Course.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //                    ModifiedAt = cs.Course.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //                    PublishedAt = cs.Course.PublishedAt.HasValue ? cs.Course.PublishedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
        //                    ApprovedAt = cs.Course.PublishedAt.HasValue ? cs.Course.PublishedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
        //                    TemplateInfo = cs.Course.Template != null ? new TemplateInfo
        //                    {
        //                        TemplateId = cs.Course.Template.Id,
        //                        Name = cs.Course.Template.Name
        //                    } : null,
        //                    LanguageInfo = cs.Course.Language != null ? new LanguageInfo
        //                    {
        //                        Name = cs.Course.Language.LanguageName,
        //                        Code = cs.Course.Language.LanguageCode
        //                    } : null,
        //                    Goals = cs.Course.CourseGoals != null && cs.Course.CourseGoals.Any()
        //                    ? cs.Course.CourseGoals.Select(cg => new GoalResponse
        //                    {
        //                        Id = cg.Goal.Id,
        //                        Description = cg.Goal.Description,
        //                        Name = cg.Goal.Name ?? "Unknown",
        //                    }).ToList() : new List<GoalResponse>(),
        //                    TeacherInfo = cs.Course.Teacher != null ? new TeacherInfo
        //                    {
        //                        TeacherId = cs.Course.Teacher.TeacherProfileId,
        //                        FullName = cs.Course.Teacher.FullName,
        //                        Avatar = cs.Course.Teacher.Avatar,
        //                        Email = cs.Course.Teacher.Email,
        //                        PhoneNumber = cs.Course.Teacher.PhoneNumber
        //                    } : null,
        //                    ApprovedBy = cs.Course.ApprovedBy != null ? new StaffInfo
        //                    {
        //                        StaffId = cs.Course.ApprovedBy.StaffLanguageId,
        //                        UserName = cs.Course.ApprovedBy.User.UserName,
        //                        Email = cs.Course.ApprovedBy.User.Email
        //                    } : null,
        //                    Topics = cs.Course.CourseTopics.Select(t => new TopicResponse
        //                    {
        //                        TopicId = t.TopicID,
        //                        TopicName = t.Topic.Name,
        //                        TopicDescription = t.Topic.Description ?? "Unknown",
        //                        ImageUrl = t.Topic.ImageUrl ?? "Unknown"
        //                    }).ToList(),
        //                    NumLessons = cs.Course.NumLessons
        //                }
        //            })
        //            .ToListAsync();

        //        return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Success(submissions, totalRecords, request.Page, request.PageSize);
        //    }
        //    catch (Exception ex)
        //    {
        //        return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Error("An unexpected error occurred.", 500, ex.Message);
        //    }
        //}
        //public async Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetAllCourseSubmissionsByTeacherAsync(Guid userId, PagingRequest request, string status)
        //{
        //    try
        //    {
        //        var teacher = await _unit.TeacherProfiles.Query()
        //            .FirstOrDefaultAsync(x => x.UserId == userId);

        //        if (teacher == null)
        //            return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Fail(null, "Teacher does not exist.", 404);

        //        SubmissionStatus? filterStatus = null;
        //        if (!string.IsNullOrWhiteSpace(status))
        //        {
        //            if (Enum.TryParse<SubmissionStatus>(status, true, out var parsedStatus))
        //                filterStatus = parsedStatus;
        //            else
        //                return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Fail(null, "Invalid submission status.", 400);
        //        }

        //        var query = _unit.CourseSubmissions.Query()
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.Language)
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.Template)
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.CourseGoals)
        //                    .ThenInclude(cg => cg.Goal)
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.CourseTopics)
        //            .Include(cs => cs.Course)
        //                .ThenInclude(c => c.ApprovedBy)
        //            .Where(cs => cs.SubmittedById == teacher.TeacherProfileId);

        //        if (filterStatus.HasValue)
        //            query = query.Where(cs => cs.Status == filterStatus.Value);

        //        var totalRecords = await query.CountAsync();

        //        var submissions = await query
        //            .OrderByDescending(cs => cs.SubmittedAt)
        //            .Skip((request.Page - 1) * request.PageSize)
        //            .Take(request.PageSize)
        //            .Select(cs => new CourseSubmissionResponse
        //            {
        //                CourseSubmissionID = cs.CourseSubmissionID,
        //                SubmissionStatus = cs.Status.ToString(),
        //                Feedback = cs.Feedback,
        //                SubmittedAt = cs.SubmittedAt,
        //                ReviewedAt = cs.ReviewedAt,
        //                Course = new CourseResponse
        //                {
        //                    CourseID = cs.Course.CourseID,
        //                    Title = cs.Course.Title,
        //                    Description = cs.Course.Description,
        //                    ImageUrl = cs.Course.ImageUrl,
        //                    Price = cs.Course.Price,
        //                    DiscountPrice = cs.Course.DiscountPrice,
        //                    CourseType = cs.Course.Type.ToString(),
        //                    CourseLevel = cs.Course.Level.ToString(),
        //                    Status = cs.Course.Status.ToString(),
        //                    CreatedAt = cs.Course.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //                    ModifiedAt = cs.Course.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //                    PublishedAt = cs.Course.PublishedAt.HasValue ? cs.Course.PublishedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
        //                    ApprovedAt = cs.Course.PublishedAt.HasValue ? cs.Course.PublishedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
        //                    TemplateInfo = cs.Course.Template != null ? new TemplateInfo
        //                    {
        //                        TemplateId = cs.Course.Template.Id,
        //                        Name = cs.Course.Template.Name
        //                    } : null,
        //                    LanguageInfo = cs.Course.Language != null ? new LanguageInfo
        //                    {
        //                        Name = cs.Course.Language.LanguageName,
        //                        Code = cs.Course.Language.LanguageCode
        //                    } : null,
        //                    Goals = cs.Course.CourseGoals != null && cs.Course.CourseGoals.Any()
        //                    ? cs.Course.CourseGoals.Select(cg => new GoalResponse
        //                    {
        //                        Id = cg.Goal.Id,
        //                        Description = cg.Goal.Description,
        //                        Name = cg.Goal.Name ?? "Unknown",
        //                    }).ToList() : new List<GoalResponse>(),
        //                    TeacherInfo = new TeacherInfo
        //                    {
        //                        TeacherId = teacher.TeacherProfileId,
        //                        FullName = teacher.FullName,
        //                        Avatar = teacher.Avatar,
        //                        Email = teacher.Email,
        //                        PhoneNumber = teacher.PhoneNumber
        //                    },
        //                    ApprovedBy = cs.Course.ApprovedBy != null ? new StaffInfo
        //                    {
        //                        StaffId = cs.Course.ApprovedBy.StaffLanguageId,
        //                        UserName = cs.Course.ApprovedBy.User.UserName,
        //                        Email = cs.Course.ApprovedBy.User.Email
        //                    } : null,
        //                    Topics = cs.Course.CourseTopics.Select(t => new TopicResponse
        //                    {
        //                        TopicId = t.TopicID,
        //                        TopicName = t.Topic.Name,
        //                        TopicDescription = t.Topic.Description ?? "Unknown",
        //                        ImageUrl = t.Topic.ImageUrl ?? "Unknown"
        //                    }).ToList(),
        //                    NumLessons = cs.Course.NumLessons
        //                }
        //            })
        //            .ToListAsync();

        //        return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Success(submissions, totalRecords, request.Page, request.PageSize);
        //    }
        //    catch (Exception ex)
        //    {
        //        return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Error("An unexpected error occurred.", 500, ex.Message);
        //    }
        //}

        //public async Task<BaseResponse<CourseResponse>> GetCourseByIdAsync(Guid courseId)
        //{
        //    var course = await _unit.Courses.Query()
        //        .Include(c => c.Template)
        //        .Include(c => c.Teacher)
        //        .Include(c => c.Language)
        //        .Include(c => c.CourseGoals)
        //            .ThenInclude(cg => cg.Goal)
        //        .Include(c => c.CourseTopics)
        //            .ThenInclude(ct => ct.Topic)
        //        .FirstOrDefaultAsync(c => c.CourseID == courseId);

        //    if (course == null)
        //        return BaseResponse<CourseResponse>.Fail(null, "Course not found.", 404);

        //    var response = new CourseResponse
        //    {
        //        CourseID = course.CourseID,
        //        Title = course.Title,
        //        Description = course.Description,
        //        ImageUrl = course.ImageUrl,
        //        Price = course.Price,
        //        DiscountPrice = course.DiscountPrice,
        //        CourseType = course.Type.ToString(),
        //        CourseLevel = course.Level.ToString(),
        //        Status = course.Status.ToString(),
        //        CreatedAt = course.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //        ModifiedAt = course.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //        NumLessons = course.NumLessons,

        //        TemplateInfo = course.Template != null
        //            ? new TemplateInfo
        //            {
        //                TemplateId = course.Template.Id,
        //                Name = course.Template.Name
        //            }
        //            : null,

        //        TeacherInfo = course.Teacher != null
        //            ? new TeacherInfo
        //            {
        //                TeacherId = course.Teacher.TeacherProfileId,
        //                FullName = course.Teacher.FullName ?? "Unknown",
        //                Avatar = course.Teacher.Avatar ?? "Unknown",
        //                Email = course.Teacher.Email ?? "Unknown",
        //                PhoneNumber = course.Teacher.PhoneNumber ?? "Unknown"
        //            }
        //            : null,

        //        LanguageInfo = course.Language != null
        //            ? new LanguageInfo
        //            {
        //                Name = course.Language.LanguageName ?? "Unknown",
        //                Code = course.Language.LanguageCode ?? "Unknown"
        //            }
        //            : null,

        //        Goals = course.CourseGoals != null && course.CourseGoals.Any()
        //            ? course.CourseGoals.Select(cg => new GoalResponse
        //            {
        //                Id = cg.Goal.Id,
        //                Name = cg.Goal.Name,
        //                Description = cg.Goal.Description
        //            }).ToList()
        //            : new List<GoalResponse>(),

        //        Topics = course.CourseTopics != null && course.CourseTopics.Any()
        //            ? course.CourseTopics.Select(ct => new TopicResponse
        //            {
        //                TopicId = ct.Topic.TopicID,
        //                TopicName = ct.Topic.Name,
        //                TopicDescription = ct.Topic.Description ?? string.Empty,
        //                ImageUrl = ct.Topic.ImageUrl ?? string.Empty
        //            }).ToList()
        //            : new List<TopicResponse>()
        //    };

        //    return BaseResponse<CourseResponse>.Success(response, "Course retrieved successfully.");
        //}

        //public async Task<BaseResponse<object>> RejectCourseSubmissionAsync(Guid userId, Guid submissionId, string reason)
        //{
        //    try
        //    {
        //        var staff = await _unit.StaffLanguages.FindAsync(x => x.UserId == userId);
        //        if (staff == null)
        //            return BaseResponse<object>.Fail(null, "Staff does not exist.", 404);

        //        var submission = await _unit.CourseSubmissions.Query()
        //            .Include(cs => cs.Course)
        //            .FirstOrDefaultAsync(cs => cs.CourseSubmissionID == submissionId);

        //        if (submission == null)
        //            return BaseResponse<object>.Fail(null, "Course submission does not exist.", 404);

        //        if (staff.LanguageId != submission.Course.LanguageId)
        //            return BaseResponse<object>.Fail(null, "You are not authorized to review submissions in this language.", 403);

        //        if (submission.Status != SubmissionStatus.Pending)
        //            return BaseResponse<object>.Fail(null, "This submission has already been reviewed and cannot be rejected again.", 400);

        //        submission.Status = SubmissionStatus.Rejected;
        //        submission.ReviewedById = staff.StaffLanguageId;
        //        submission.ReviewedAt = TimeHelper.GetVietnamTime();
        //        submission.Feedback = reason;

        //        submission.Course.Status = CourseStatus.Rejected;
        //        submission.Course.UpdatedAt = TimeHelper.GetVietnamTime();

        //        await _unit.CourseSubmissions.UpdateAsync(submission);
        //        await _unit.Courses.UpdateAsync(submission.Course);
        //        await _unit.SaveChangesAsync();

        //        return BaseResponse<object>.Success(new { submissionId = submission.CourseSubmissionID }, "Course submission rejected successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BaseResponse<object>.Fail(null, $"An unexpected error occurred: {ex.Message}", 500);
        //    }
        //}

        //public async Task<BaseResponse<object>> SubmitCourseForReviewAsync(Guid userId, Guid courseId)
        //{
        //    try
        //    {
        //        var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
        //        if (teacher == null)
        //            return BaseResponse<object>.Fail(null, "Teacher does not exist.", 404);

        //        var course = await _unit.Courses
        //            .Query()
        //            .Include(c => c.Template)
        //            .Include(c => c.Language)
        //            .Include(c => c.CourseGoals)
        //            .Include(c => c.CourseTopics)
        //            .Include(c => c.CourseUnits)
        //                .ThenInclude(u => u.Lessons)
        //                    .ThenInclude(l => l.Exercises)
        //            .FirstOrDefaultAsync(c => c.CourseID == courseId);

        //        if (course == null)
        //            return BaseResponse<object>.Fail(null, "Course does not exist.", 404);

        //        if (course.TeacherId != teacher.TeacherProfileId)
        //            return BaseResponse<object>.Fail(null, "You do not have permission to submit this course.", 403);

        //        if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
        //            return BaseResponse<object>.Fail(null, "Course can only be submitted if it is in Pending or Rejected status.", 400);

        //        var template = course.Template;
        //        if (template == null)
        //            return BaseResponse<object>.Fail(null, "This course does not have a valid template assigned.", 400);

        //        var errors = new List<string>();

        //        if (template.RequireLang && course.LanguageId == null)
        //            errors.Add("This course must specify a language.");

        //        if (template.RequireGoal && (course.CourseGoals == null || !course.CourseGoals.Any()))
        //            errors.Add("This course must include at least one goal.");

        //        if (template.RequireTopic && (course.CourseTopics == null || !course.CourseTopics.Any()))
        //            errors.Add("This course must include at least one topic.");

        //        if (template.RequireLevel && course.Level == null)
        //            errors.Add("This course must specify a level.");

        //        var unitCount = course.CourseUnits?.Count ?? 0;
        //        if (unitCount < template.MinUnits)
        //            errors.Add($"This course must contain at least {template.MinUnits} units.");

        //        foreach (var unit in course.CourseUnits ?? Enumerable.Empty<DAL.Models.CourseUnit>())
        //        {
        //            var lessonCount = unit.Lessons?.Count ?? 0;
        //            if (lessonCount < template.MinLessonsPerUnit)
        //            {
        //                errors.Add($"Unit '{unit.Title}' must contain at least {template.MinLessonsPerUnit} lessons.");
        //                continue;
        //            }

        //            foreach (var lesson in unit.Lessons ?? Enumerable.Empty<DAL.Models.Lesson>())
        //            {
        //                var exerciseCount = lesson.Exercises?.Count ?? 0;
        //                if (exerciseCount < template.MinExercisesPerLesson)
        //                    errors.Add($"Lesson '{lesson.Title}' must contain at least {template.MinExercisesPerLesson} exercises.");
        //            }
        //        }

        //        if (errors.Any())
        //        {
        //            return BaseResponse<object>.Fail(
        //                new { Details = errors },
        //                "Course does not meet the template requirements.",
        //                400
        //            );
        //        }

        //        var submission = new CourseSubmission
        //        {
        //            CourseSubmissionID = Guid.NewGuid(),
        //            CourseID = courseId,
        //            SubmittedById = teacher.TeacherProfileId,
        //            Status = SubmissionStatus.Pending,
        //            SubmittedAt = TimeHelper.GetVietnamTime(),
        //        };

        //        course.Status = CourseStatus.PendingApproval;
        //        course.UpdatedAt = TimeHelper.GetVietnamTime();

        //        await _unit.CourseSubmissions.CreateAsync(submission);
        //        await _unit.SaveChangesAsync();

        //        return BaseResponse<object>.Success(new { submissionId = submission.CourseSubmissionID }, "Course submitted for review successfully.");

        //    }
        //    catch (Exception ex)
        //    {
        //        return BaseResponse<object>.Error("error", 500, $"An error occurred: {ex.Message}");
        //    }
        //}

        //public async Task<BaseResponse<CourseResponse>> UpdateCourseAsync(Guid userId, Guid courseId, UpdateCourseRequest request)
        //{
        //    // Validation container
        //    var validationErrors = new Dictionary<string, string>();

        //    try
        //    {
        //        // 1. Find teacher (caller)
        //        var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
        //        if (teacher == null)
        //            return BaseResponse<CourseResponse>.Fail("Teacher does not exist.");

        //        // 2. Load existing course
        //        var course = await _unit.Courses.GetByIdAsync(courseId);
        //        if (course == null)
        //            return BaseResponse<CourseResponse>.Fail("Course does not exist.");

        //        // 3. Permission: only owner teacher can update (adjust if staff/admin allowed)
        //        if (course.TeacherId != teacher.TeacherProfileId)
        //            return BaseResponse<CourseResponse>.Fail("You do not have permission to update this course.");

        //        if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
        //        {
        //            return BaseResponse<CourseResponse>.Fail(
        //                $"Only courses with status '{CourseStatus.Draft.ToString()}' or '{CourseStatus.Rejected.ToString()}' can be updated.",
        //                400.ToString()
        //            );
        //        }
        //        // 4. Determine effective template: use request.TemplateId if provided, otherwise the current one
        //        var effectiveTemplateId = request.TemplateId ?? course.TemplateId;
        //        if (effectiveTemplateId == Guid.Empty)
        //            return BaseResponse<CourseResponse>.Fail("TemplateId is required.");

        //        var template = await _unit.CourseTemplates.GetByIdAsync(effectiveTemplateId);
        //        if (template == null)
        //            return BaseResponse<CourseResponse>.Fail("Template does not exist.");

        //        List<Guid> topicIds = new();
        //        var existingCourseTopicEntities = (List<DAL.Models.CourseTopic>)await _unit.CourseTopics.FindAllAsync(ct => ct.CourseID == course.CourseID);
        //        var existingTopicIds = existingCourseTopicEntities?.Select(t => t.TopicID).ToList() ?? new List<Guid>();

        //        if (!string.IsNullOrWhiteSpace(request.TopicIds))
        //        {
        //            topicIds = request.TopicIds
        //                .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //                .Select(x => Guid.TryParse(x.Trim(), out var id) ? id : Guid.Empty)
        //                .Where(x => x != Guid.Empty)
        //                .Distinct()
        //                .ToList();
        //        }
        //        else
        //        {
        //            topicIds = existingTopicIds;
        //        }

        //        // 6. Validate Goal (based on effective template requirement)
        //        List<int> goalIds = new();
        //        var existingCourseGoalEntities = (List<DAL.Models.CourseGoal>)await _unit.CourseGoals.FindAllAsync(g => g.CourseId == course.CourseID);
        //        var existingGoalIds = existingCourseGoalEntities?.Select(g => g.GoalId).ToList() ?? new List<int>();

        //        if (!string.IsNullOrWhiteSpace(request.GoalIds))
        //        {
        //            goalIds = request.GoalIds
        //                .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //                .Select(x => int.TryParse(x.Trim(), out var gid) ? gid : 0)
        //                .Where(gid => gid > 0)
        //                .Distinct()
        //                .ToList();
        //        }
        //        else
        //        {
        //            goalIds = existingGoalIds;
        //        }

        //        // 7. Validate Level (based on template)
        //        if (template.RequireLevel)
        //        {
        //            if (request.Level.HasValue)
        //            {
        //                if (!Enum.IsDefined(typeof(LevelType), request.Level.Value))
        //                {
        //                    validationErrors[nameof(request.Level)] = "Invalid or missing level. Must be one of: Beginner, Intermediate, or Advanced.";
        //                }
        //            }
        //        }
        //        else
        //        {
        //            // if template doesn't require level but request provided one, validate it
        //            if (request.Level.HasValue && !Enum.IsDefined(typeof(LevelType), request.Level.Value))
        //            {
        //                validationErrors[nameof(request.Level)] = "Invalid level value.";
        //            }
        //        }

        //        if (template.RequireGoal && !goalIds.Any())
        //            validationErrors[nameof(request.GoalIds)] = "At least one goal is required according to the course template.";

        //        // 8. Validate topics according to template
        //        if (template.RequireTopic && !topicIds.Any())
        //            validationErrors[nameof(request.TopicIds)] = "At least one topic is required according to the course template.";

        //        // 9. Validate topic ids exist (if any)
        //        var validTopics = new List<DAL.Models.Topic>();
        //        if (topicIds.Any())
        //        {
        //            validTopics = (List<DAL.Models.Topic>)await _unit.Topics.FindAllAsync(t => topicIds.Contains(t.TopicID));
        //            var validIds = validTopics.Select(t => t.TopicID).ToHashSet();
        //            var invalidIds = topicIds.Where(id => !validIds.Contains(id)).ToList();
        //            if (invalidIds.Any())
        //                return BaseResponse<CourseResponse>.Fail($"Invalid topic IDs: {string.Join(", ", invalidIds)}");
        //        }

        //        var validGoals = new List<DAL.Models.Goal>();
        //        if (goalIds.Any())
        //        {
        //            validGoals = (List<DAL.Models.Goal>)await _unit.Goals.FindAllAsync(g => goalIds.Contains(g.Id));
        //            var validGoalIds = validGoals.Select(g => g.Id).ToHashSet();
        //            var invalidGoals = goalIds.Where(id => !validGoalIds.Contains(id)).ToList();
        //            if (invalidGoals.Any())
        //                validationErrors[nameof(request.GoalIds)] = $"Invalid goal IDs: {string.Join(", ", invalidGoals)}";
        //        }

        //        // 10. Type/Price validation (combine effective type)
        //        var effectiveType = request.Type ?? course.Type;
        //        var effectivePrice = request.Price;
        //        var effectiveDiscount = request.DiscountPrice.HasValue ? request.DiscountPrice.Value : course.DiscountPrice;

        //        switch (effectiveType)
        //        {
        //            case CourseType.Free:
        //                if (effectivePrice != 0)
        //                    validationErrors[nameof(request.Price)] = "Price must be 0 for free courses.";
        //                if (effectiveDiscount.HasValue && effectiveDiscount.Value != 0)
        //                    validationErrors[nameof(request.DiscountPrice)] = "DiscountPrice must be 0 for free courses.";
        //                break;
        //            case CourseType.Paid:
        //                if (effectivePrice <= 0)
        //                    validationErrors[nameof(request.Price)] = "Price must be greater than 0 for paid courses.";
        //                break;
        //            default:
        //                validationErrors[nameof(request.Type)] = "Invalid course type.";
        //                break;
        //        }

        //        // 11. If any validation errors -> return 400-like fail with details
        //        if (validationErrors.Any())
        //        {
        //            return BaseResponse<CourseResponse>.Fail(
        //                validationErrors,
        //                "Course request does not satisfy the selected course template requirements.",
        //                400
        //            );
        //        }

        //        // 12. If a new image provided -> upload
        //        if (request.Image != null)
        //        {
        //            if (!string.IsNullOrWhiteSpace(course.PublicId))
        //            {
        //                var deleteResult = await _cloudinaryService.DeleteFileAsync(course.PublicId);
        //                if (!deleteResult)
        //                {
        //                    _logger?.LogWarning($"Failed to delete old image from Cloudinary for course {course.CourseID} (PublicId: {course.PublicId})");
        //                }
        //            }

        //            var uploadResult = await _cloudinaryService.UploadImageAsync(request.Image);

        //            if (uploadResult == null || string.IsNullOrWhiteSpace(uploadResult.Url))
        //            {
        //                return BaseResponse<CourseResponse>.Fail("Failed when uploading the new image.");
        //            }

        //            course.ImageUrl = uploadResult.Url;
        //            course.PublicId = string.IsNullOrWhiteSpace(uploadResult.PublicId)
        //                ? Guid.NewGuid().ToString()
        //                : uploadResult.PublicId;

        //            _logger?.LogInformation($"Updated image for course {course.CourseID}. New PublicId: {course.PublicId}");
        //        }


        //        // 13. Apply updates (only when request provides a change)
        //        // Title & Description (trim + length limits)
        //        if (!string.IsNullOrWhiteSpace(request.Title))
        //        {
        //            var t = request.Title.Trim();
        //            course.Title = t.Length > 200 ? t.Substring(0, 200) : t;
        //        }

        //        if (!string.IsNullOrWhiteSpace(request.Description))
        //        {
        //            var d = request.Description.Trim();
        //            course.Description = d.Length > 1000 ? d.Substring(0, 1000) : d;
        //        }

        //        // Template change
        //        if (request.TemplateId.HasValue && request.TemplateId.Value != Guid.Empty)
        //            course.TemplateId = request.TemplateId.Value;

        //        // Level
        //        if (request.Level.HasValue)
        //            course.Level = request.Level.Value;

        //        // Type
        //        course.Type = effectiveType;

        //        // Price & Discount
        //        // Note: Price in DTO is non-nullable and required; we update Price to provided value.
        //        // Replace this line:
        //        // course.Price = effectivePrice < 0 ? course.Price : effectivePrice;

        //        // With this fix:
        //        course.Price = (effectivePrice.HasValue && effectivePrice.Value >= 0) ? effectivePrice.Value : course.Price;
        //        course.DiscountPrice = request.DiscountPrice is null or < 0 ? course.DiscountPrice : request.DiscountPrice;

        //        // Topics: if request.TopicIds is not null => replace current topics with provided (possibly empty to clear)
        //        if (request.TopicIds != null)
        //        {
        //            // remove existing CourseTopics and create new set from topicIds
        //            course.CourseTopics = topicIds.Select(tid => new DAL.Models.CourseTopic
        //            {
        //                CourseTopicID = Guid.NewGuid(),
        //                CourseID = course.CourseID,
        //                TopicID = tid
        //            }).ToList();
        //        }
        //        if (request.GoalIds != null)
        //        {
        //            course.CourseGoals = goalIds.Select(gid => new DAL.Models.CourseGoal
        //            {
        //                CourseGoalId = Guid.NewGuid(),
        //                CourseId = course.CourseID,
        //                GoalId = gid
        //            }).ToList();
        //        }
        //        // else keep existing course.CourseTopics


        //        if (course.Status == CourseStatus.Rejected)
        //        {
        //            course.Status = CourseStatus.Draft;
        //        }

        //        // 14. UpdatedAt
        //        course.UpdatedAt = TimeHelper.GetVietnamTime();

        //        // 15. Persist update
        //        var updateResult = await _unit.Courses.UpdateAsync(course);
        //        if (updateResult <= 0)
        //        {
        //            return BaseResponse<CourseResponse>.Fail("Unable to update the course. Please try again.");
        //        }

        //        // 16. Prepare DTO response (fetch related info similar to Create)
        //        Language? language = null;
        //        if (teacher.LanguageId != Guid.Empty)
        //            language = await _unit.Languages.GetByIdAsync(teacher.LanguageId);

        //        // topics for response: validTopics already contains the topics for topicIds when topicIds.Any()
        //        // but if topicIds was empty and course had no topics, validTopics will be empty -> that's fine.
        //        if (!validTopics.Any() && topicIds.Any())
        //        {
        //            // fallback: fetch them
        //            validTopics = (List<DAL.Models.Topic>)await _unit.Topics.FindAllAsync(t => topicIds.Contains(t.TopicID));
        //        }


        //        var responseDto = new CourseResponse
        //        {
        //            CourseID = course.CourseID,
        //            Title = course.Title,
        //            Description = course.Description,
        //            ImageUrl = course.ImageUrl,
        //            Price = course.Price,
        //            DiscountPrice = course.DiscountPrice,
        //            CourseType = course.Type.ToString(),
        //            CourseLevel = course.Level.ToString(),
        //            Status = course.Status.ToString(),
        //            CreatedAt = course.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //            ModifiedAt = course.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        //            NumLessons = course.NumLessons,
        //            TemplateInfo = template != null ? new TemplateInfo
        //            {
        //                TemplateId = template.Id,
        //                Name = template.Name
        //            } : null,
        //            TeacherInfo = new TeacherInfo
        //            {
        //                TeacherId = teacher.TeacherProfileId,
        //                FullName = teacher.FullName ?? "Unknown",
        //                Avatar = teacher.Avatar ?? "Unknown",
        //                Email = teacher.Email ?? "Unknown",
        //                PhoneNumber = teacher.PhoneNumber ?? "Unknown",
        //            },
        //            LanguageInfo = new LanguageInfo
        //            {
        //                Name = language?.LanguageName ?? "Unknown",
        //                Code = language?.LanguageCode ?? "Unknown"
        //            },
        //            Goals = validGoals.Select(g => new GoalResponse
        //            {
        //                Id = g.Id,
        //                Name = g.Name,
        //                Description = g.Description
        //            }).ToList(),
        //            Topics = validTopics.Select(t => new TopicResponse
        //            {
        //                TopicId = t.TopicID,
        //                TopicName = t.Name,
        //                TopicDescription = t.Description ?? string.Empty,
        //                ImageUrl = t.ImageUrl ?? string.Empty
        //            }).ToList()
        //        };

        //        return BaseResponse<CourseResponse>.Success(responseDto, "Course updated successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError(ex, "An unexpected error occurred while updating a course.");
        //        return BaseResponse<CourseResponse>.Error("An unexpected error occurred while updating the course. Please try again later.");
        //    }
        //}
    }
}
