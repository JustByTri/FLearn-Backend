using BLL.IServices.Teacher;
using Common.DTO.ApiResponse;
using Common.DTO.Teacher.Response;
using DAL.UnitOfWork;

namespace BLL.Services.Teacher
{
    public class TeacherService : ITeacherService
    {
        private readonly IUnitOfWork _unit;
        public TeacherService(IUnitOfWork unit)
        {
            _unit = unit;
        }
        public async Task<BaseResponse<TeacherProfileResponse>> GetTeacherProfileAsync(Guid userId)
        {
            try
            {
                var user = await _unit.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<TeacherProfileResponse>.Fail(new object(), "Accessed denied", 403);

                var teacher = await _unit.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return BaseResponse<TeacherProfileResponse>.Fail(new object(), "Accessed denied", 403);

                var language = await _unit.Languages.GetByIdAsync(teacher.LanguageId);

                var response = new TeacherProfileResponse
                {
                    TeacherId = teacher.TeacherId,
                    Language = language?.LanguageName,
                    FullName = teacher.FullName,
                    DateOfBirth = teacher.BirthDate.ToString("dd-MM-yyyy"),
                    Bio = teacher.Bio,
                    Avatar = teacher.Avatar,
                    Email = teacher.Email,
                    PhoneNumber = teacher.PhoneNumber,
                    ProficiencyCode = teacher.ProficiencyCode,
                    AverageRating = teacher.AverageRating,
                    ReviewCount = teacher.ReviewCount,
                    MeetingUrl = teacher.MeetingUrl,
                };

                return BaseResponse<TeacherProfileResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return BaseResponse<TeacherProfileResponse>.Fail(new object(), ex.Message, 500);
            }

        }
    }
}
