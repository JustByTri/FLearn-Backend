using Common.DTO.ApiResponse;
using Common.DTO.Exercise.Request;
using Common.DTO.Exercise.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Exercise
{
    public interface IExerciseService
    {
        Task<BaseResponse<ExerciseResponse>> CreateExerciseAsync(Guid userId, Guid lessonId, ExerciseRequest request);
        Task<BaseResponse<ExerciseResponse>> UpdateExerciseAsync(Guid userId, Guid lessonId, Guid exerciseId, ExerciseUpdateRequest request);
        Task<PagedResponse<IEnumerable<ExerciseResponse>>> GetExercisesByLessonIdAsync(Guid lessonId, PagingRequest request);
        Task<BaseResponse<ExerciseResponse>> GetExerciseByIdAsync(Guid exerciseId);
        Task<BaseResponse<ExerciseResponse>> DeleteExerciseByIdAsync(Guid userId, Guid exerciseId);
    }
}
