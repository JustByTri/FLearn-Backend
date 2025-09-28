using Common.DTO.ApiResponse;
using Common.DTO.Exercise.Request;
using Common.DTO.Exercise.Response;

namespace BLL.IServices.Exercise
{
    public interface IExerciseService
    {
        Task<BaseResponse<ExerciseResponse>> CreateExerciseAsync(Guid teacherId, Guid courseId, Guid unitId, Guid lessonId, ExerciseRequest request);
    }
}
