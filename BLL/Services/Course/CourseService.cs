using BLL.IServices.Course;
using BLL.IServices.Upload;
using Common.DTO.ApiResponse;
using Common.DTO.Course.Request;
using Common.DTO.Course.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Topic.Response;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Courses
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

        public async Task<BaseResponse<CourseResponse>> CreateCourseAsync(Guid teacherId, CourseRequest request)
        {

            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
            {
                return BaseResponse<CourseResponse>.Fail("Invalid TeacherID. Teacher not found or does not have role 'Teacher'.");
            }

            var template = await _unit.CourseTemplates.Query()
                    .FirstOrDefaultAsync(t => t.Id == request.TemplateId);

            if (template == null)
            {
                return BaseResponse<CourseResponse>.Fail("Selected template not found.");
            }

            var language = await _unit.Languages.Query()
                .Where(l => l.LanguageID == request.LanguageID)
                .Select(l => new { l.LanguageID, l.LanguageName, l.LanguageCode })
                .FirstOrDefaultAsync();

            if (language == null)
            {
                return BaseResponse<CourseResponse>.Fail("Selected language not found.");
            }

            var goal = await _unit.Goals.Query()
                .Where(g => g.Id == request.GoalId)
                .Select(g => new { g.Id, g.Name, g.Description })
                .FirstOrDefaultAsync();

            if (goal == null)
            {
                return BaseResponse<CourseResponse>.Fail("Selected goal not found.");
            }

            try
            {
                var missing = new List<string>();
                if (template.RequireGoal && request.GoalId == 0)
                    missing.Add("Course Goal is required by the template.");

                if (template.RequireLevel && request.CourseLevel == null)
                    missing.Add("Course Level is required by the template.");

                if (template.RequireSkillFocus && request.CourseSkill == null)
                    missing.Add("Primary Skill is required by the template.");

                if (template.RequireTopic && (request.TopicIds == null || !request.TopicIds.Any()))
                    missing.Add("At least one topic must be selected according to the template.");

                if (template.RequireLang && request.LanguageID == Guid.Empty)
                    missing.Add("Course Language is required by the template.");

                if (missing.Any())
                {
                    var msg = "Unable to create the course. Please provide the following information: "
                              + string.Join(" ", missing);
                    return BaseResponse<CourseResponse>.Fail(msg);
                }

                if (request.CourseType == DAL.Type.CourseType.Free)
                {
                    if (request.Price > 0)
                        return BaseResponse<CourseResponse>.Fail("The course is selected as Free, so Price must be 0.");
                    if (request.DiscountPrice.HasValue && request.DiscountPrice.Value > 0)
                        return BaseResponse<CourseResponse>.Fail("Discount cannot be set for a Free course (must be 0).");
                }
                else
                {
                    if (request.Price < 20000)
                        return BaseResponse<CourseResponse>.Fail("Paid course must have a price >= 20,000 VND.");
                    if (request.DiscountPrice.HasValue && request.DiscountPrice.Value >= request.Price)
                        return BaseResponse<CourseResponse>.Fail("DiscountPrice must be less than Price.");
                }

                var result = await _cloudinaryService.UploadImageAsync(request.Image);

                if (result.Url == null)
                {
                    return BaseResponse<CourseResponse>.Fail("Failed when uploading the image.");
                }

                var topicEntities = new List<DAL.Models.Topic>();
                if (request.TopicIds != null && request.TopicIds.Any())
                {
                    topicEntities = await _unit.Topics.Query()
                        .Where(t => request.TopicIds.Contains(t.TopicID))
                        .ToListAsync();

                    var missingTopicIds = request.TopicIds.Except(topicEntities.Select(t => t.TopicID)).ToList();
                    if (missingTopicIds.Any())
                    {
                        return BaseResponse<CourseResponse>.Fail($"Topic(s) with id: {string.Join(',', missingTopicIds)} not found.");
                    }
                }
                var nowUtc = DateTime.UtcNow;
                var newCourse = new Course
                {
                    CourseID = Guid.NewGuid(),
                    Title = request.Title,
                    Description = request.Description,
                    ImageUrl = result.Url,
                    PublicId = result.PublicId,
                    Price = request.Price,
                    DiscountPrice = request.DiscountPrice,
                    CourseType = request.CourseType,
                    CourseLevel = request.CourseLevel ?? default,
                    CourseSkill = request.CourseSkill ?? default,
                    TemplateId = request.TemplateId,
                    TeacherID = teacherId,
                    LanguageID = request.LanguageID,
                    GoalId = request.GoalId,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc,
                    Status = CourseStatus.Draft
                };

                if (topicEntities.Any())
                {
                    foreach (var t in topicEntities)
                    {
                        newCourse.CourseTopics.Add(new DAL.Models.CourseTopic
                        {
                            CourseTopicID = Guid.NewGuid(),
                            CourseID = newCourse.CourseID,
                            TopicID = t.TopicID
                        });
                    }
                }

                var saveResult = await _unit.Courses.CreateAsync(newCourse);
                if (saveResult <= 0)
                {
                    return BaseResponse<CourseResponse>.Fail("Unable to save the course. Please try again.");
                }

                var templateDto = new TemplateInfo
                {
                    Id = template.Id,
                    Name = template.Name,
                };

                var teacherDto = new TeacherInfo
                {
                    Id = teacher.UserID,
                    FullName = GetUserDisplayName(teacher),
                    AvatarUrl = GetUserAvatarUrl(teacher)
                };

                var languageDto = language == null ? null : new LanguageInfo
                {
                    Id = language.LanguageID,
                    Name = language.LanguageName,
                    Code = language.LanguageCode
                };

                var goalDto = goal == null ? null : new GoalInfo
                {
                    Id = goal.Id,
                    Name = goal.Name,
                    Description = goal.Description
                };

                var response = new CourseResponse
                {
                    CourseID = newCourse.CourseID,
                    Title = newCourse.Title,
                    Description = newCourse.Description,
                    ImageUrl = newCourse.ImageUrl,
                    TemplateInfo = templateDto,
                    Price = newCourse.Price,
                    DiscountPrice = newCourse.DiscountPrice,
                    CourseType = newCourse.CourseType.ToString(),
                    TeacherInfo = teacherDto,
                    LanguageInfo = languageDto,
                    GoalInfo = goalDto,
                    CourseLevel = newCourse.CourseLevel.ToString(),
                    CourseSkill = newCourse.CourseSkill.ToString(),
                    NumLessons = 0,
                    Status = newCourse.Status.ToString(),
                    CreatedAt = newCourse.CreatedAt.ToString("dd/MM/yyyy"),
                    ModifiedAt = newCourse.UpdatedAt.ToString("dd/MM/yyyy"),
                    Topics = topicEntities.Select(t => new TopicResponse
                    {
                        TopicId = t.TopicID,
                        TopicName = t.Name ?? string.Empty,
                        TopicDescription = t.Description ?? string.Empty,
                        ImageUrl = t.ImageUrl ?? string.Empty
                    }).ToList()
                };

                return BaseResponse<CourseResponse>.Success(response, "Course created successfully.");

            }
            catch (Exception ex)
            {
                return BaseResponse<CourseResponse>.Fail($"Error: {ex.Message}");
            }
        }

        public async Task<BaseResponse<CourseResponse>> GetCourseByIdAsync(Guid courseId)
        {
            var c = await _unit.Courses.Query()
               .Include(c => c.Language)
               .Include(c => c.Goal)
               .Include(c => c.Teacher)
               .Include(c => c.CourseTemplate)
               .Include(c => c.CourseTopics)
                   .ThenInclude(ct => ct.Topic)
               .FirstOrDefaultAsync(c => c.CourseID == courseId);

            if (c == null)
                return BaseResponse<CourseResponse>.Fail("Course not found", 404.ToString());

            var response = new CourseResponse
            {
                CourseID = c.CourseID,
                Title = c.Title,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                Price = c.Price,
                DiscountPrice = c.DiscountPrice,
                CourseType = c.CourseType.ToString(),
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt.ToString("dd/MM/yyyy"),
                ModifiedAt = c.UpdatedAt.ToString("dd/MM/yyyy"),
                PublishedAt = c.PublishedAt.HasValue ? c.PublishedAt.Value.ToString("dd/MM/yyyy") : null,
                ApprovedAt = c.PublishedAt.HasValue ? c.PublishedAt.Value.ToString("dd/MM/yyyy") : null,
                CourseLevel = c.CourseLevel.ToString(),
                CourseSkill = c.CourseSkill.ToString(),
                NumLessons = c.NumLessons,

                TeacherInfo = c.Teacher != null ? new TeacherInfo
                {
                    Id = c.Teacher.UserID,
                    FullName = c.Teacher.UserName,
                    AvatarUrl = c.Teacher.ProfilePictureUrl
                } : null,

                LanguageInfo = c.Language != null ? new LanguageInfo
                {
                    Id = c.Language.LanguageID,
                    Name = c.Language.LanguageName,
                    Code = c.Language.LanguageCode
                } : null,

                TemplateInfo = c.CourseTemplate != null ? new TemplateInfo
                {
                    Id = c.CourseTemplate.Id,
                    Name = c.CourseTemplate.Name
                } : null,

                GoalInfo = c.Goal != null ? new GoalInfo
                {
                    Id = c.Goal.Id,
                    Name = c.Goal.Name,
                    Description = c.Goal.Description
                } : null,

                ApprovedBy = c.Staff != null ? new UserInfo
                {
                    Id = c.Staff.UserID,
                    FullName = c.Staff.UserName,
                    AvatarUrl = c.Staff.ProfilePictureUrl
                } : null,

                Topics = c.CourseTopics != null
                    ? c.CourseTopics.Select(ct => new TopicResponse
                    {
                        TopicId = ct.Topic.TopicID,
                        TopicName = ct.Topic.Name,
                        TopicDescription = ct.Topic.Description,
                        ImageUrl = ct.Topic.ImageUrl
                    }).ToList()
                    : new List<TopicResponse>()
            };

            return BaseResponse<CourseResponse>.Success(response);
        }

        public async Task<PagedResponse<IEnumerable<CourseResponse>>> GetAllCoursesAsync(PagingRequest request)
        {
            var query = _unit.Courses.Query()
                .Include(c => c.Language)
                .Include(c => c.Goal)
                .Include(c => c.Teacher)
                .Include(c => c.CourseTemplate)
                .Include(c => c.CourseTopics)
                    .ThenInclude(ct => ct.Topic)
                .AsNoTracking();


            var totalItems = await query.CountAsync();

            var courses = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var mapped = courses.Select(c => new CourseResponse
            {
                CourseID = c.CourseID,
                Title = c.Title,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                Price = c.Price,
                DiscountPrice = c.DiscountPrice,
                CourseType = c.CourseType.ToString(),
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt.ToString("dd/MM/yyyy"),
                ModifiedAt = c.UpdatedAt.ToString("dd/MM/yyyy"),
                PublishedAt = c.PublishedAt.HasValue ? c.PublishedAt.Value.ToString("dd/MM/yyyy") : null,
                ApprovedAt = c.PublishedAt.HasValue ? c.PublishedAt.Value.ToString("dd/MM/yyyy") : null,
                CourseLevel = c.CourseLevel.ToString(),
                CourseSkill = c.CourseSkill.ToString(),
                NumLessons = c.NumLessons,
                TeacherInfo = c.Teacher != null ? new TeacherInfo
                {
                    Id = c.Teacher.UserID,
                    FullName = c.Teacher.UserName,
                    AvatarUrl = c.Teacher.ProfilePictureUrl
                } : null,

                LanguageInfo = c.Language != null ? new LanguageInfo
                {
                    Id = c.Language.LanguageID,
                    Name = c.Language.LanguageName,
                    Code = c.Language.LanguageCode
                } : null,

                TemplateInfo = c.CourseTemplate != null ? new TemplateInfo
                {
                    Id = c.CourseTemplate.Id,
                    Name = c.CourseTemplate.Name
                } : null,

                GoalInfo = c.Goal != null ? new GoalInfo
                {
                    Id = c.Goal.Id,
                    Name = c.Goal.Name,
                    Description = c.Goal.Description
                } : null,

                ApprovedBy = c.Staff != null ? new UserInfo
                {
                    Id = c.Staff.UserID,
                    FullName = c.Staff.UserName,
                    AvatarUrl = c.Staff.ProfilePictureUrl
                } : null,

                Topics = c.CourseTopics != null ? c.CourseTopics.Select(t => new TopicResponse
                {
                    TopicId = t.TopicID,
                    TopicName = t.Topic.Name,
                    TopicDescription = t.Topic.Description,
                    ImageUrl = t.Topic.ImageUrl
                }).ToList() : new List<TopicResponse>()
            }).ToList();

            return PagedResponse<IEnumerable<CourseResponse>>.Success(
                mapped,
                request.Page,
                request.PageSize,
                totalItems
            );
        }

        public async Task<BaseResponse<CourseResponse>> UpdateCourseAsync(Guid teacherId, Guid courseId, UpdateCourseRequest request)
        {
            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null || !teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
            {
                return BaseResponse<CourseResponse>.Fail("Invalid TeacherID. Teacher not found or does not have role 'Teacher'.");
            }

            var template = await _unit.CourseTemplates.Query()
                    .FirstOrDefaultAsync(t => t.Id == request.TemplateId);

            if (template == null)
            {
                return BaseResponse<CourseResponse>.Fail("Selected template not found.");
            }

            var language = await _unit.Languages.Query()
                .Where(l => l.LanguageID == request.LanguageID)
                .Select(l => new { l.LanguageID, l.LanguageName, l.LanguageCode })
                .FirstOrDefaultAsync();

            if (language == null)
            {
                return BaseResponse<CourseResponse>.Fail("Selected language not found.");
            }

            var goal = await _unit.Goals.Query()
                .Where(g => g.Id == request.GoalId)
                .Select(g => new { g.Id, g.Name, g.Description })
                .FirstOrDefaultAsync();

            if (goal == null)
            {
                return BaseResponse<CourseResponse>.Fail("Selected goal not found.");
            }

            try
            {
                var course = await _unit.Courses.Query()
                    .Include(c => c.CourseTopics)
                    .FirstOrDefaultAsync(c => c.CourseID == courseId && c.TeacherID == teacherId);

                if (course == null)
                {
                    return BaseResponse<CourseResponse>.Fail("Course not found or you are not the owner.");
                }

                if (course.Status != CourseStatus.Draft && course.Status != CourseStatus.Rejected)
                {
                    return BaseResponse<CourseResponse>.Fail("Only Draft or Rejected courses can be updated.");
                }

                var missing = new List<string>();
                if (template.RequireGoal && request.GoalId == 0)
                    missing.Add("Course Goal is required by the template.");

                if (template.RequireLevel && request.CourseLevel == null)
                    missing.Add("Course Level is required by the template.");

                if (template.RequireSkillFocus && request.CourseSkill == null)
                    missing.Add("Primary Skill is required by the template.");

                if (template.RequireTopic && (request.TopicIds == null || !request.TopicIds.Any()))
                    missing.Add("At least one topic must be selected according to the template.");

                if (template.RequireLang && request.LanguageID == Guid.Empty)
                    missing.Add("Course Language is required by the template.");

                if (missing.Any())
                {
                    var msg = "Unable to update the course. Missing: " + string.Join(" ", missing);
                    return BaseResponse<CourseResponse>.Fail(msg);
                }

                if (request.CourseType == DAL.Type.CourseType.Free)
                {
                    if (request.Price > 0)
                        return BaseResponse<CourseResponse>.Fail("Free course must have Price = 0.");
                    if (request.DiscountPrice.HasValue && request.DiscountPrice.Value > 0)
                        return BaseResponse<CourseResponse>.Fail("Discount cannot be set for Free course.");
                }
                else
                {
                    if (request.Price < 20000)
                        return BaseResponse<CourseResponse>.Fail("Paid course must have a price >= 20,000 VND.");
                    if (request.DiscountPrice.HasValue && request.DiscountPrice.Value >= request.Price)
                        return BaseResponse<CourseResponse>.Fail("DiscountPrice must be less than Price.");
                }

                if (request.Image != null)
                {
                    if (!string.IsNullOrEmpty(course.PublicId))
                    {
                        await _cloudinaryService.DeleteFileAsync(course.PublicId);
                    }

                    var result = await _cloudinaryService.UploadImageAsync(request.Image);
                    if (result.Url == null)
                        return BaseResponse<CourseResponse>.Fail("Failed when uploading the image.");

                    course.ImageUrl = result.Url;
                    course.PublicId = result.PublicId;
                }


                var existingTopics = await _unit.CourseTopics.Query()
                    .Where(ct => ct.CourseID == course.CourseID)
                    .ToListAsync();

                if (existingTopics.Any())
                {
                    _unit.CourseTopics.RemoveRange(existingTopics);
                    await _unit.SaveChangesAsync();
                }

                if (request.TopicIds != null && request.TopicIds.Any())
                {
                    var topicEntities = await _unit.Topics.Query()
                        .Where(t => request.TopicIds.Contains(t.TopicID))
                        .ToListAsync();

                    var missingTopicIds = request.TopicIds.Except(topicEntities.Select(t => t.TopicID)).ToList();
                    if (missingTopicIds.Any())
                    {
                        return BaseResponse<CourseResponse>.Fail(
                            $"Topic(s) with id: {string.Join(',', missingTopicIds)} not found.");
                    }

                    var newCourseTopics = topicEntities.Select(t => new DAL.Models.CourseTopic
                    {
                        CourseTopicID = Guid.NewGuid(),
                        CourseID = course.CourseID,
                        TopicID = t.TopicID
                    });

                    await _unit.CourseTopics.AddRangeAsync(newCourseTopics);
                    await _unit.SaveChangesAsync();
                }

                course.Title = request.Title;
                course.Description = request.Description;
                course.Price = request.Price;
                course.DiscountPrice = request.DiscountPrice;
                course.CourseType = request.CourseType;
                course.CourseLevel = request.CourseLevel ?? default;
                course.CourseSkill = request.CourseSkill ?? default;
                course.TemplateId = request.TemplateId;
                course.LanguageID = request.LanguageID;
                course.GoalId = request.GoalId;
                course.UpdatedAt = DateTime.UtcNow;
                course.Status = CourseStatus.Draft;

                var saveResult = await _unit.Courses.UpdateAsync(course);
                if (saveResult <= 0)
                {
                    return BaseResponse<CourseResponse>.Fail("Unable to update the course. Please try again.");
                }

                var templateDto = new TemplateInfo { Id = template.Id, Name = template.Name };
                var teacherDto = new TeacherInfo
                {
                    Id = teacher.UserID,
                    FullName = GetUserDisplayName(teacher),
                    AvatarUrl = GetUserAvatarUrl(teacher)
                };
                var languageDto = language == null ? null : new LanguageInfo
                {
                    Id = language.LanguageID,
                    Name = language.LanguageName,
                    Code = language.LanguageCode
                };
                var goalDto = goal == null ? null : new GoalInfo
                {
                    Id = goal.Id,
                    Name = goal.Name,
                    Description = goal.Description
                };

                var response = new CourseResponse
                {
                    CourseID = course.CourseID,
                    Title = course.Title,
                    Description = course.Description,
                    ImageUrl = course.ImageUrl,
                    TemplateInfo = templateDto,
                    Price = course.Price,
                    DiscountPrice = course.DiscountPrice,
                    CourseType = course.CourseType.ToString(),
                    TeacherInfo = teacherDto,
                    LanguageInfo = languageDto,
                    GoalInfo = goalDto,
                    CourseLevel = course.CourseLevel.ToString(),
                    CourseSkill = course.CourseSkill.ToString(),
                    NumLessons = course.CourseUnits.Count,
                    Status = course.Status.ToString(),
                    CreatedAt = course.CreatedAt.ToString("dd/MM/yyyy"),
                    ModifiedAt = course.UpdatedAt.ToString("dd/MM/yyyy"),
                    Topics = course.CourseTopics.Select(ct => new TopicResponse
                    {
                        TopicId = ct.TopicID,
                        TopicName = ct.Topic?.Name ?? string.Empty,
                        TopicDescription = ct.Topic?.Description ?? string.Empty,
                        ImageUrl = ct.Topic?.ImageUrl ?? string.Empty
                    }).ToList()
                };

                return BaseResponse<CourseResponse>.Success(response, "Course updated successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<CourseResponse>.Fail($"Error: {ex.Message}");
            }
        }
        private string GetUserDisplayName(DAL.Models.User user)
        {
            if (!string.IsNullOrWhiteSpace(user.FullName)) return user.FullName;
            if (!string.IsNullOrWhiteSpace(user.UserName)) return user.UserName;
            return user.Email ?? "Unknown";
        }
        private string? GetUserAvatarUrl(DAL.Models.User user)
        {
            if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl)) return user.ProfilePictureUrl;
            if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl)) return user.ProfilePictureUrl;
            return null;
        }

        public async Task<PagedResponse<IEnumerable<CourseResponse>>> GetAllCoursesByTeacherIdAsync(Guid teacherId, PagingRequest request)
        {
            var teacher = await _unit.Users.Query()
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == teacherId);

            if (teacher == null)
            {
                throw new KeyNotFoundException($"Teacher with ID {teacherId} not found.");
            }

            if (!teacher.UserRoles.Any(ur => ur.Role.Name == "Teacher"))
            {
                throw new UnauthorizedAccessException($"User with ID {teacherId} is not a teacher.");
            }

            var query = _unit.Courses.Query()
                .Where(c => c.TeacherID == teacherId)
                .Include(c => c.Language)
                .Include(c => c.Goal)
                .Include(c => c.Teacher)
                .Include(c => c.CourseTemplate)
                .Include(c => c.CourseTopics)
                    .ThenInclude(ct => ct.Topic)
                .AsNoTracking();


            var totalItems = await query.CountAsync();

            var courses = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var mapped = courses.Select(c => new CourseResponse
            {
                CourseID = c.CourseID,
                Title = c.Title,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                Price = c.Price,
                DiscountPrice = c.DiscountPrice,
                CourseType = c.CourseType.ToString(),
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt.ToString("dd/MM/yyyy"),
                ModifiedAt = c.UpdatedAt.ToString("dd/MM/yyyy"),
                PublishedAt = c.PublishedAt.HasValue ? c.PublishedAt.Value.ToString("dd/MM/yyyy") : null,
                ApprovedAt = c.PublishedAt.HasValue ? c.PublishedAt.Value.ToString("dd/MM/yyyy") : null,
                CourseLevel = c.CourseLevel.ToString(),
                CourseSkill = c.CourseSkill.ToString(),
                NumLessons = c.NumLessons,
                TeacherInfo = c.Teacher != null ? new TeacherInfo
                {
                    Id = c.Teacher.UserID,
                    FullName = c.Teacher.UserName,
                    AvatarUrl = c.Teacher.ProfilePictureUrl
                } : null,

                LanguageInfo = c.Language != null ? new LanguageInfo
                {
                    Id = c.Language.LanguageID,
                    Name = c.Language.LanguageName,
                    Code = c.Language.LanguageCode
                } : null,

                TemplateInfo = c.CourseTemplate != null ? new TemplateInfo
                {
                    Id = c.CourseTemplate.Id,
                    Name = c.CourseTemplate.Name
                } : null,

                GoalInfo = c.Goal != null ? new GoalInfo
                {
                    Id = c.Goal.Id,
                    Name = c.Goal.Name,
                    Description = c.Goal.Description
                } : null,

                ApprovedBy = c.Staff != null ? new UserInfo
                {
                    Id = c.Staff.UserID,
                    FullName = c.Staff.UserName,
                    AvatarUrl = c.Staff.ProfilePictureUrl
                } : null,

                Topics = c.CourseTopics != null ? c.CourseTopics.Select(t => new TopicResponse
                {
                    TopicId = t.TopicID,
                    TopicName = t.Topic.Name,
                    TopicDescription = t.Topic.Description,
                    ImageUrl = t.Topic.ImageUrl
                }).ToList() : new List<TopicResponse>()
            }).ToList();

            return PagedResponse<IEnumerable<CourseResponse>>.Success(
                mapped,
                request.Page,
                request.PageSize,
                totalItems
            );
        }
    }
}
