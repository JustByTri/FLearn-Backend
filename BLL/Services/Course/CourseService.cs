using BLL.IServices.Course;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Course;
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
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BLL.Services.Course
{
    public class CourseService : ICourseService
    {
        private readonly IUnitOfWork _unit;
        private readonly ICloudinaryService _cloudinaryService;
        public CourseService(IUnitOfWork unit, ICloudinaryService cloudinaryService)
        {
            _unit = unit;
            _cloudinaryService = cloudinaryService;
        }
        public async Task<BaseResponse<object>> ApproveCourseSubmissionAsync(Guid userId, Guid submissionId)
        {
            try
            {
                var manager = await _unit.ManagerLanguages.FindAsync(x => x.UserId == userId);
                if (manager == null)
                    return BaseResponse<object>.Fail(null, "Access denied", 403);

                var submission = await _unit.CourseSubmissions.Query()
                    .Include(cs => cs.Course)
                    .FirstOrDefaultAsync(cs => cs.CourseSubmissionID == submissionId);

                if (submission == null)
                    return BaseResponse<object>.Fail(null, "Course submission does not exist.", 404);

                if (manager.LanguageId != submission.Course.LanguageId)
                    return BaseResponse<object>.Fail(null, "You are not authorized to approve submissions in this language.", 403);

                if (submission.Status != SubmissionStatus.Pending)
                    return BaseResponse<object>.Fail(null, "This submission has already been reviewed and cannot be approved again.", 400);

                submission.Status = SubmissionStatus.Approved;
                submission.ReviewedById = manager.ManagerId;
                submission.ReviewedAt = TimeHelper.GetVietnamTime();
                submission.Course.Status = CourseStatus.Published;
                submission.Course.ApprovedByID = manager.ManagerId;
                submission.Course.PublishedAt = TimeHelper.GetVietnamTime();
                submission.Course.UpdatedAt = TimeHelper.GetVietnamTime();
                await _unit.CourseSubmissions.UpdateAsync(submission);
                await _unit.Courses.UpdateAsync(submission.Course);
                await _unit.SaveChangesAsync();
                return BaseResponse<object>.Success(new { submissionId = submission.CourseSubmissionID }, "Course submission approved successfully.");

            }
            catch (Exception ex)
            {
                return BaseResponse<object>.Fail(null, $"An unexpected error occurred: {ex.Message}", 500);
            }
        }
        public async Task<BaseResponse<CourseResponse>> CreateCourseAsync(Guid userId, CourseRequest request)
        {
            return await _unit.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);

                    if (teacher == null)
                        return BaseResponse<CourseResponse>.Fail(new object(), "Access denied", 403);

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
                    var lesson = new DAL.Models.Lesson
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
                        TotalExercises = 0,
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
                    CourseUnitID = l.CourseUnitID,
                    UnitTitle = unit.Title,
                    CourseID = unit.CourseID,
                    CourseTitle = course.Title,
                    Description = l.Description,
                    Position = l.Position,
                    TotalExercises = l.TotalExercises,
                    CreatedAt = l.CreatedAt.ToString("dd-MM-yyyy")
                }).ToList();

                unitResponses.Add(new UnitResponse
                {
                    CourseUnitID = unit.CourseUnitID,
                    Title = unit.Title,
                    CourseID = unit.CourseID,
                    CourseTitle = course.Title,
                    Description = unit.Description,
                    Position = unit.Position,
                    TotalLessons = unit.TotalLessons ?? 0,
                    IsPreview = unit.IsPreview ?? false,
                    CreatedAt = unit.CreatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAt = unit.UpdatedAt.ToString("dd-MM-yyyy"),
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
        public async Task<PagedResponse<IEnumerable<CourseResponse>>> GetCoursesAsync(PagingRequest request, string status, string lang)
        {
            try
            {
                if (request.Page <= 0) request.Page = 1;
                if (request.PageSize <= 0) request.PageSize = 10;

                var query = _unit.Courses.Query()
                    .Include(c => c.Template)
                        .ThenInclude(t => t.Program)
                            .ThenInclude(p => p.Levels)
                    .Include(c => c.Teacher)
                    .Include(c => c.Language)
                    .Include(c => c.CourseTopics)
                        .ThenInclude(ct => ct.Topic)
                    .AsSplitQuery()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse<CourseStatus>(status, true, out var parsedStatus))
                    {
                        query = query.Where(c => c.Status == parsedStatus);
                    }
                    else
                    {
                        return PagedResponse<IEnumerable<CourseResponse>>.Fail(
                            errors: new object(),
                            message: $"Invalid course status: '{status}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(CourseStatus)))}",
                            code: 400
                        );
                    }
                }

                if (!string.IsNullOrWhiteSpace(lang))
                {
                    query = query.Where(c => c.Language.LanguageCode.ToLower() == lang.ToLower());
                }

                var totalCount = await query.CountAsync();

                var skip = (request.Page - 1) * request.PageSize;
                var courses = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToListAsync();

                var responses = new List<CourseResponse>();
                foreach (var course in courses)
                {
                    var response = new CourseResponse
                    {
                        CourseId = course.CourseID,
                        TemplateId = course.TemplateId,
                        Language = course.Language.LanguageName,
                        Program = new Common.DTO.Course.Response.Program
                        {
                            ProgramId = course.ProgramId,
                            Name = course.Program.Name,
                            Description = course.Program.Description,
                            Level = new Common.DTO.Course.Response.Level
                            {
                                LevelId = course.LevelId,
                                Name = course.Level.Name,
                                Description = course.Level.Description
                            }
                        },
                        Teacher = new Common.DTO.Course.Response.Teacher
                        {
                            TeacherId = course.TeacherId,
                            Avatar = course.Teacher.Avatar,
                            Name = course.Teacher.FullName,
                            Email = course.Teacher.Email
                        },
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
                        Topics = course.CourseTopics?
                                        .Select(ct => new TopicResponse
                                        {
                                            TopicId = ct.Topic.TopicID,
                                            TopicName = ct.Topic.Name ?? string.Empty,
                                            TopicDescription = ct.Topic.Description ?? string.Empty,
                                            ImageUrl = ct.Topic.ImageUrl ?? string.Empty
                                        })
                                        .ToList() ?? new List<TopicResponse>(),
                    };
                    responses.Add(response);
                }
                return PagedResponse<IEnumerable<CourseResponse>>.Success(
                    responses,
                    request.Page,
                    request.PageSize,
                    totalCount);
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<CourseResponse>>.Error($"An unexpected error occurred while fetching courses. Please try again later.\\{ex.Message}");
            }
        }
        public async Task<PagedResponse<IEnumerable<CourseResponse>>> GetCoursesByTeacherAsync(Guid userId, PagingRequest request, string status)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null)
                {
                    return PagedResponse<IEnumerable<CourseResponse>>.Fail(
                        new object(),
                        "Access denied",
                        403
                    );
                }

                if (request.Page <= 0) request.Page = 1;
                if (request.PageSize <= 0) request.PageSize = 10;

                var query = _unit.Courses.Query()
                    .Where(c => c.TeacherId == teacher.TeacherId)
                    .AsQueryable();

                var hasAnyCourses = await query.AnyAsync();
                if (!hasAnyCourses)
                {
                    return PagedResponse<IEnumerable<CourseResponse>>.Success(
                        new List<CourseResponse>(),
                        request.Page,
                        request.PageSize,
                        0
                    );
                }

                query = query
                    .Include(c => c.ApprovedBy)
                        .ThenInclude(m => m.User)
                    .Include(c => c.Template)
                        .ThenInclude(t => t.Program)
                            .ThenInclude(p => p.Levels)
                    .Include(c => c.Teacher)
                    .Include(c => c.Language)
                    .Include(c => c.CourseTopics)
                        .ThenInclude(ct => ct.Topic)
                    .AsSplitQuery();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse<CourseStatus>(status, true, out var parsedStatus))
                    {
                        query = query.Where(c => c.Status == parsedStatus);
                    }
                    else
                    {
                        return PagedResponse<IEnumerable<CourseResponse>>.Fail(
                            errors: new object(),
                            message: $"Invalid course status: '{status}'. Allowed values: {string.Join(", ", Enum.GetNames(typeof(CourseStatus)))}",
                            code: 400
                        );
                    }
                }

                var totalCount = await query.CountAsync();

                var skip = (request.Page - 1) * request.PageSize;
                var courses = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToListAsync();

                var responses = courses.Select(course => new CourseResponse
                {
                    CourseId = course.CourseID,
                    TemplateId = course.TemplateId,
                    Language = course.Language?.LanguageName ?? string.Empty,
                    Program = course.Template?.Program != null ? new Common.DTO.Course.Response.Program
                    {
                        ProgramId = course.Template.Program.ProgramId,
                        Name = course.Template.Program.Name ?? string.Empty,
                        Description = course.Template.Program.Description ?? string.Empty,
                        Level = course.Level != null ? new Common.DTO.Course.Response.Level
                        {
                            LevelId = course.Level.LevelId,
                            Name = course.Level.Name ?? string.Empty,
                            Description = course.Level.Description ?? string.Empty
                        } : null
                    } : null,
                    ApprovedBy = course.ApprovedBy != null ? new Common.DTO.Course.Response.ApprovedBy
                    {
                        ManagerId = course.ApprovedBy.ManagerId,
                        Name = course.ApprovedBy.User?.UserName ?? string.Empty,
                        Email = course.ApprovedBy.User?.Email ?? string.Empty,
                    } : null,
                    Title = course.Title ?? string.Empty,
                    Description = course.Description ?? string.Empty,
                    LearningOutcome = course.LearningOutcome ?? string.Empty,
                    ImageUrl = course.ImageUrl ?? string.Empty,
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
                    Topics = course.CourseTopics?
                        .Where(ct => ct.Topic != null)
                        .Select(ct => new TopicResponse
                        {
                            TopicId = ct.Topic.TopicID,
                            TopicName = ct.Topic.Name ?? string.Empty,
                            TopicDescription = ct.Topic.Description ?? string.Empty,
                            ImageUrl = ct.Topic.ImageUrl ?? string.Empty
                        })
                        .ToList() ?? new List<TopicResponse>(),
                }).ToList();

                return PagedResponse<IEnumerable<CourseResponse>>.Success(
                    responses,
                    request.Page,
                    request.PageSize,
                    totalCount);
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<CourseResponse>>.Error(
                    $"An unexpected error occurred while fetching courses. Please try again later. {ex.Message}"
                );
            }
        }
        public async Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetCourseSubmissionsByTeacherAsync(Guid userId, PagingRequest request, string status)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);

                if (teacher == null)
                    return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Fail(new object(), "Access denied", 403);

                SubmissionStatus? filterStatus = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse<SubmissionStatus>(status, true, out var parsedStatus))
                        filterStatus = parsedStatus;
                    else
                        return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Fail(new object(), "Invalid submission status.", 400);
                }

                var query = _unit.CourseSubmissions.Query()
                    .Include(cs => cs.Course)
                        .ThenInclude(c => c.Language)
                    .Include(cs => cs.Course)
                        .ThenInclude(c => c.Program)
                    .Include(cs => cs.Course)
                        .ThenInclude(c => c.Level)
                    .Include(cs => cs.Course)
                        .ThenInclude(c => c.Teacher)
                    .Where(cs => cs.SubmittedById == teacher.TeacherId);

                if (filterStatus.HasValue)
                    query = query.Where(cs => cs.Status == filterStatus.Value);

                var totalRecords = await query.CountAsync();

                var submissions = await query
                    .OrderByDescending(cs => cs.SubmittedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(cs => new CourseSubmissionResponse
                    {
                        SubmissionId = cs.CourseSubmissionID,
                        SubmissionStatus = cs.Status.ToString(),
                        Feedback = cs.Feedback,
                        SubmittedAt = cs.SubmittedAt.ToString("dd-MM-yyyy"),
                        Course = new CourseResponse
                        {
                            CourseId = cs.Course.CourseID,
                            TemplateId = cs.Course.TemplateId,
                            Language = cs.Course.Language != null ? cs.Course.Language.LanguageName : null,
                            Program = cs.Course.Program != null ? new Common.DTO.Course.Response.Program
                            {
                                ProgramId = cs.Course.Program.ProgramId,
                                Name = cs.Course.Program.Name,
                                Description = cs.Course.Program.Description,
                                Level = cs.Course.Level != null ? new Common.DTO.Course.Response.Level
                                {
                                    LevelId = cs.Course.Level.LevelId,
                                    Description = cs.Course.Level.Description,
                                    Name = cs.Course.Level.Name,
                                } : null
                            } : null,
                            Teacher = cs.Course.Teacher != null ? new Common.DTO.Course.Response.Teacher
                            {
                                TeacherId = cs.Course.Teacher.TeacherId,
                                Avatar = cs.Course.Teacher.Avatar,
                                Email = cs.Course.Teacher.Email,
                                Name = cs.Course.Teacher.FullName,
                            } : null,
                            Title = cs.Course.Title,
                            Description = cs.Course.Description,
                            LearningOutcome = cs.Course.LearningOutcome,
                            ImageUrl = cs.Course.ImageUrl,
                            Price = cs.Course.Price,
                            DiscountPrice = cs.Course.DiscountPrice,
                            CourseType = cs.Course.CourseType.ToString(),
                            GradingType = cs.Course.GradingType.ToString(),
                            LearnerCount = cs.Course.LearnerCount,
                            AverageRating = cs.Course.AverageRating,
                            ReviewCount = cs.Course.ReviewCount,
                            NumLessons = cs.Course.NumLessons,
                            NumUnits = cs.Course.NumUnits,
                            DurationDays = cs.Course.DurationDays,
                            EstimatedHours = cs.Course.EstimatedHours,
                            CourseStatus = cs.Course.Status.ToString(),
                            PublishedAt = cs.Course.PublishedAt != null ? cs.Course.PublishedAt.Value.ToString("dd-MM-yyyy") : null,
                            CreatedAt = cs.Course.CreatedAt.ToString("dd-MM-yyyy"),
                            ModifiedAt = cs.Course.UpdatedAt.ToString("dd-MM-yyyy"),
                            Topics = new List<TopicResponse>(),
                            Units = new List<UnitResponse>()
                        }
                    })
                    .ToListAsync();

                return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Success(submissions, totalRecords, request.Page, request.PageSize);
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Error("An unexpected error occurred.", 500, ex.Message);
            }
        }
        public async Task<BaseResponse<object>> RejectCourseSubmissionAsync(Guid userId, Guid submissionId, string reason)
        {
            try
            {
                var manager = await _unit.ManagerLanguages.FindAsync(x => x.UserId == userId);
                if (manager == null)
                    return BaseResponse<object>.Fail(null, "Access denied", 403);

                var submission = await _unit.CourseSubmissions.Query()
                    .Include(cs => cs.Course)
                    .FirstOrDefaultAsync(cs => cs.CourseSubmissionID == submissionId);

                if (submission == null)
                    return BaseResponse<object>.Fail(null, "Course submission does not exist.", 404);

                if (manager.LanguageId != submission.Course.LanguageId)
                    return BaseResponse<object>.Fail(null, "You are not authorized to review submissions in this language.", 403);

                if (submission.Status != SubmissionStatus.Pending)
                    return BaseResponse<object>.Fail(null, "This submission has already been reviewed and cannot be rejected again.", 400);

                submission.Status = SubmissionStatus.Rejected;
                submission.ReviewedById = manager.ManagerId;
                submission.ReviewedAt = TimeHelper.GetVietnamTime();
                submission.Feedback = reason;
                submission.Course.Status = CourseStatus.Rejected;
                submission.Course.UpdatedAt = TimeHelper.GetVietnamTime();
                await _unit.CourseSubmissions.UpdateAsync(submission);
                await _unit.Courses.UpdateAsync(submission.Course);
                await _unit.SaveChangesAsync();
                return BaseResponse<object>.Success(new { submissionId = submission.CourseSubmissionID }, "Course submission rejected successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<object>.Fail(null, $"An unexpected error occurred: {ex.Message}", 500);
            }
        }
        public async Task<BaseResponse<object>> SubmitCourseForReviewAsync(Guid userId, Guid courseId)
        {
            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null)
                    return BaseResponse<object>.Fail(null, "Access denied", 403);

                var course = await _unit.Courses.Query()
                    .Include(c => c.Template)
                    .Include(c => c.Language)
                    .Include(c => c.CourseTopics)
                    .Include(c => c.CourseUnits)
                        .ThenInclude(u => u.Lessons)
                            .ThenInclude(l => l.Exercises)
                    .FirstOrDefaultAsync(c => c.CourseID == courseId);

                if (course == null)
                    return BaseResponse<object>.Fail(null, "Course does not exist.", 404);

                if (course.TeacherId != teacher.TeacherId)
                    return BaseResponse<object>.Fail(null, "You do not have permission to submit this course.", 403);

                if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
                    return BaseResponse<object>.Fail(null, "Course can only be submitted if it is in Pending or Rejected status.", 400);

                var template = course.Template;
                if (template == null)
                    return BaseResponse<object>.Fail(null, "This course does not have a valid template assigned.", 400);

                var errors = new List<string>();

                var unitCount = course.CourseUnits?.Count ?? 0;
                if (unitCount < template.UnitCount)
                    errors.Add($"This course must contain at least {template.UnitCount} units.");

                foreach (var unit in course.CourseUnits ?? Enumerable.Empty<DAL.Models.CourseUnit>())
                {
                    var lessonCount = unit.Lessons?.Count ?? 0;
                    if (lessonCount < template.LessonsPerUnit)
                    {
                        errors.Add($"Unit '{unit.Title}' must contain at least {template.LessonsPerUnit} lessons.");
                        continue;
                    }

                    foreach (var lesson in unit.Lessons ?? Enumerable.Empty<DAL.Models.Lesson>())
                    {
                        var exerciseCount = lesson.Exercises?.Count ?? 0;
                        if (exerciseCount < template.ExercisesPerLesson)
                            errors.Add($"Lesson '{lesson.Title}' must contain at least {template.ExercisesPerLesson} exercises.");
                    }
                }

                if (errors.Any())
                {
                    return BaseResponse<object>.Fail(
                        new { Details = errors },
                        "Course does not meet the template requirements.",
                        400
                    );
                }

                var submission = new CourseSubmission
                {
                    CourseSubmissionID = Guid.NewGuid(),
                    CourseID = courseId,
                    SubmittedById = teacher.TeacherId,
                    Status = SubmissionStatus.Pending,
                    SubmittedAt = TimeHelper.GetVietnamTime(),
                };

                course.Status = CourseStatus.PendingApproval;
                course.UpdatedAt = TimeHelper.GetVietnamTime();

                await _unit.CourseSubmissions.CreateAsync(submission);
                await _unit.SaveChangesAsync();
                return BaseResponse<object>.Success(new { submissionId = submission.CourseSubmissionID }, "Course submitted for review successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<object>.Error("error", 500, $"An error occurred: {ex.Message}");
            }
        }
        public async Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetCourseSubmissionsByManagerAsync(Guid userId, PagingRequest request, string status)
        {
            try
            {
                var manager = await _unit.ManagerLanguages.Query()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (manager == null)
                    return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Fail(null, "Access denied", 403);

                SubmissionStatus? filterStatus = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse<SubmissionStatus>(status, true, out var parsedStatus))
                        filterStatus = parsedStatus;
                    else
                        return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Fail(new object(), "Invalid submission status.", 400);
                }

                var query = _unit.CourseSubmissions.Query()
                    .Include(cs => cs.Course)
                        .ThenInclude(c => c.Language)
                    .Include(cs => cs.Course)
                        .ThenInclude(c => c.Program)
                    .Include(cs => cs.Course)
                        .ThenInclude(c => c.Level)
                    .Include(cs => cs.Course)
                        .ThenInclude(c => c.Teacher)
                    .Include(cs => cs.SubmittedBy)
                    .Include(cs => cs.ReviewedBy)
                        .ThenInclude(rb => rb.User)
                    .Where(cs => cs.Course.LanguageId == manager.LanguageId);

                if (filterStatus.HasValue)
                    query = query.Where(cs => cs.Status == filterStatus.Value);

                var totalRecords = await query.CountAsync();

                var submissions = await query
                    .OrderByDescending(cs => cs.SubmittedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(cs => new CourseSubmissionResponse
                    {
                        SubmissionId = cs.CourseSubmissionID,
                        SubmissionStatus = cs.Status.ToString(),
                        Feedback = cs.Feedback,
                        SubmittedAt = cs.SubmittedAt.ToString("dd-MM-yyyy"),
                        ReviewedAt = cs.ReviewedAt.HasValue ? cs.ReviewedAt.Value.ToString("dd-MM-yyyy") : null,
                        Course = new CourseResponse
                        {
                            CourseId = cs.Course.CourseID,
                            TemplateId = cs.Course.TemplateId,
                            Language = cs.Course.Language != null ? cs.Course.Language.LanguageName : null,
                            Program = cs.Course.Program != null ? new Common.DTO.Course.Response.Program
                            {
                                ProgramId = cs.Course.Program.ProgramId,
                                Name = cs.Course.Program.Name,
                                Description = cs.Course.Program.Description,
                                Level = cs.Course.Level != null ? new Common.DTO.Course.Response.Level
                                {
                                    LevelId = cs.Course.Level.LevelId,
                                    Description = cs.Course.Level.Description,
                                    Name = cs.Course.Level.Name,
                                } : null
                            } : null,
                            Teacher = cs.Course.Teacher != null ? new Common.DTO.Course.Response.Teacher
                            {
                                TeacherId = cs.Course.Teacher.TeacherId,
                                Avatar = cs.Course.Teacher.Avatar,
                                Email = cs.Course.Teacher.Email,
                                Name = cs.Course.Teacher.FullName,
                            } : null,
                            Title = cs.Course.Title,
                            Description = cs.Course.Description,
                            LearningOutcome = cs.Course.LearningOutcome,
                            ImageUrl = cs.Course.ImageUrl,
                            Price = cs.Course.Price,
                            DiscountPrice = cs.Course.DiscountPrice,
                            CourseType = cs.Course.CourseType.ToString(),
                            GradingType = cs.Course.GradingType.ToString(),
                            LearnerCount = cs.Course.LearnerCount,
                            AverageRating = cs.Course.AverageRating,
                            ReviewCount = cs.Course.ReviewCount,
                            NumLessons = cs.Course.NumLessons,
                            NumUnits = cs.Course.NumUnits,
                            DurationDays = cs.Course.DurationDays,
                            EstimatedHours = cs.Course.EstimatedHours,
                            CourseStatus = cs.Course.Status.ToString(),
                            PublishedAt = cs.Course.PublishedAt.HasValue ? cs.Course.PublishedAt.Value.ToString("dd-MM-yyyy") : null,
                            CreatedAt = cs.Course.CreatedAt.ToString("dd-MM-yyyy"),
                            ModifiedAt = cs.Course.UpdatedAt.ToString("dd-MM-yyyy"),
                            Topics = new List<TopicResponse>(),
                            Units = new List<UnitResponse>()
                        },
                        Submitter = cs.SubmittedBy != null ? new Common.DTO.Course.Response.Submitter
                        {
                            TeacherId = cs.SubmittedBy.TeacherId,
                            Name = cs.SubmittedBy.FullName,
                            Email = cs.SubmittedBy.Email,
                            Avatar = cs.SubmittedBy.Avatar,
                            PhoneNumber = cs.SubmittedBy.PhoneNumber,
                        } : null,
                        Reviewer = cs.ReviewedBy != null ? new Common.DTO.Course.Response.Reviewer
                        {
                            ManagerId = cs.ReviewedBy.ManagerId,
                            Name = cs.ReviewedBy.User != null ? cs.ReviewedBy.User.FullName : cs.ReviewedBy.User.UserName,
                            Email = cs.ReviewedBy.User != null ? cs.ReviewedBy.User.Email : "Invalid email",
                        } : null
                    })
                    .ToListAsync();
                return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Success(submissions, totalRecords, request.Page, request.PageSize);
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<CourseSubmissionResponse>>.Error("An unexpected error occurred.", 500, ex.Message);
            }
        }
        public async Task<BaseResponse<CourseResponse>> UpdateCourseAsync(Guid userId, Guid courseId, UpdateCourseRequest request)
        {
            var validationErrors = new Dictionary<string, string>();

            try
            {
                var teacher = await _unit.TeacherProfiles.FindAsync(x => x.UserId == userId);
                if (teacher == null)
                    return BaseResponse<CourseResponse>.Fail("Teacher does not exist.");

                var course = await _unit.Courses.GetByIdAsync(courseId);
                if (course == null)
                    return BaseResponse<CourseResponse>.Fail("Course does not exist.");

                if (course.TeacherId != teacher.TeacherId)
                    return BaseResponse<CourseResponse>.Fail("You do not have permission to update this course.");

                if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
                {
                    return BaseResponse<CourseResponse>.Fail(
                        $"Only courses with status '{CourseStatus.Draft.ToString()}' or '{CourseStatus.Rejected.ToString()}' can be updated.",
                        400.ToString()
                    );
                }

                DAL.Models.CourseTemplate? template = null;
                if (request.TemplateId.HasValue)
                {
                    template = await _unit.CourseTemplates.GetByIdAsync(request.TemplateId.Value);
                    if (template == null)
                        return BaseResponse<CourseResponse>.Fail("Template does not exist.");

                    if (!template.Status)
                        return BaseResponse<CourseResponse>.Fail("Template is not active.");
                }

                List<Guid> topicIds = new List<Guid>();

                if (!string.IsNullOrWhiteSpace(request.TopicIds))
                {
                    topicIds = request.TopicIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                        {
                            if (Guid.TryParse(x.Trim(), out var id))
                                return id;
                            return Guid.Empty;
                        })
                        .Where(x => x != Guid.Empty)
                        .Distinct()
                        .ToList();
                }

                if (topicIds.Any())
                {
                    var validTopics = await _unit.Topics.FindAllAsync(t => topicIds.Contains(t.TopicID));
                    var validTopicIds = validTopics.Select(t => t.TopicID).ToHashSet();
                    var invalidTopicIds = topicIds.Where(id => !validTopicIds.Contains(id)).ToList();

                    if (invalidTopicIds.Any())
                    {
                        return BaseResponse<CourseResponse>.Fail($"Invalid topic IDs: {string.Join(", ", invalidTopicIds)}");
                    }
                }

                var effectiveCourseType = request.CourseType ?? course.CourseType;
                if (effectiveCourseType == CourseType.Free)
                {
                    if (request.Price.HasValue && request.Price.Value != 0)
                        return BaseResponse<CourseResponse>.Fail("Price must be 0 for free courses.");
                }
                else if (effectiveCourseType == CourseType.Paid)
                {
                    if (request.Price.HasValue && request.Price.Value <= 0)
                        return BaseResponse<CourseResponse>.Fail("Price must be greater than 0 for paid courses.");
                }

                if (validationErrors.Any())
                {
                    return BaseResponse<CourseResponse>.Fail(
                        validationErrors,
                        "Course request does not satisfy the selected course template requirements.",
                        400
                    );
                }

                return await _unit.ExecuteInTransactionAsync(async () =>
                {
                    if (request.Image != null)
                    {
                        if (!string.IsNullOrWhiteSpace(course.PublicId))
                        {
                            var deleteResult = await _cloudinaryService.DeleteFileAsync(course.PublicId);
                            if (!deleteResult)
                            {
                                return BaseResponse<CourseResponse>.Fail($"Failed to delete old image from Cloudinary for course {course.CourseID} (PublicId: {course.PublicId})");
                            }
                        }

                        var uploadResult = await _cloudinaryService.UploadImageAsync(request.Image);
                        if (uploadResult == null || string.IsNullOrWhiteSpace(uploadResult.Url))
                        {
                            return BaseResponse<CourseResponse>.Fail("Failed to upload the new image.");
                        }

                        course.ImageUrl = uploadResult.Url;
                        course.PublicId = string.IsNullOrWhiteSpace(uploadResult.PublicId)
                            ? Guid.NewGuid().ToString()
                            : uploadResult.PublicId;
                    }

                    UpdateCourseProperties(course, request);

                    if (request.TopicIds != null)
                    {
                        await UpdateCourseTopicsAsync(course.CourseID, topicIds);
                    }

                    if (course.Status == CourseStatus.Rejected)
                    {
                        course.Status = CourseStatus.Draft;
                    }

                    course.UpdatedAt = TimeHelper.GetVietnamTime();

                    _unit.Courses.Update(course);
                    await _unit.SaveChangesAsync();

                    var updatedCourse = await GetCourseByIdAsync(course.CourseID);
                    return BaseResponse<CourseResponse>.Success(updatedCourse.Data, "Course updated successfully.", 200);
                });
            }
            catch (Exception ex)
            {
                return BaseResponse<CourseResponse>.Error("An unexpected error occurred while updating the course. Please try again later.");
            }
        }
        private void UpdateCourseProperties(DAL.Models.Course course, UpdateCourseRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Title))
                course.Title = request.Title.Trim();

            if (!string.IsNullOrWhiteSpace(request.Description))
                course.Description = request.Description.Trim();

            if (!string.IsNullOrWhiteSpace(request.LearningOutcome))
                course.LearningOutcome = request.LearningOutcome.Trim();

            if (request.TemplateId.HasValue)
                course.TemplateId = request.TemplateId.Value;

            if (request.CourseType.HasValue)
                course.CourseType = request.CourseType.Value;

            if (request.GradingType.HasValue)
                course.GradingType = request.GradingType.Value;

            if (request.Price.HasValue)
                course.Price = request.Price.Value;

            if (request.DurationDays.HasValue)
                course.DurationDays = request.DurationDays.Value;
        }
        private async Task UpdateCourseTopicsAsync(Guid courseId, List<Guid> topicIds)
        {
            var existingTopics = await _unit.CourseTopics.FindAllAsync(ct => ct.CourseID == courseId);
            foreach (var topic in existingTopics)
            {
                _unit.CourseTopics.Remove(topic);
            }

            foreach (var topicId in topicIds)
            {
                var courseTopic = new CourseTopic
                {
                    CourseTopicID = Guid.NewGuid(),
                    CourseID = courseId,
                    TopicID = topicId,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };
                await _unit.CourseTopics.CreateAsync(courseTopic);
            }
        }
        public async Task<BaseResponse<IEnumerable<PopularCourseDto>>> GetPopularCoursesAsync(int count = 10)
        {
            
            var courses = await _unit.Courses.GetPopularCoursesAsync(count);

            var popularCoursesDto = courses.Select(course => new PopularCourseDto
            {
                CourseId = course.CourseID,
                Title = course.Title,
                TeacherName = course.Teacher.FullName ?? "N/A",
                Price = course.Price,
                AverageRating = course.AverageRating,
                ReviewCount = course.ReviewCount,
                LearnerCount = course.LearnerCount,
                ImageUrl = course.ImageUrl,
                ProficiencyCode = course.Level?.Name ?? "N/A",
                ProgramName = course.Program?.Name ?? "N/A"

            });

         
            return BaseResponse<IEnumerable<PopularCourseDto>>.Success(
                popularCoursesDto,
                "Lấy danh sách khóa học phổ biến thành công.",
                (int)HttpStatusCode.OK
            );
        }
    }
}
