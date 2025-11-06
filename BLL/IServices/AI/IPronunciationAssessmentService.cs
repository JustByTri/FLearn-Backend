using Common.DTO.Assement;

namespace BLL.IServices.AI
{
    public interface IPronunciationAssessmentService
    {
        /// <summary>
        /// Đánh giá phát âm tức thời dựa trên reference text (câu chuẩn) và audio bytes.
        /// </summary>
        Task<PronunciationAssessmentResultDto> AssessAsync(byte[] audioBytes, string contentType, string referenceText, string languageCode);
    }
}
