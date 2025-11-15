using Common.DTO.ExerciseGrading.Response;
using Common.DTO.Pronunciation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Assessment
{
    public interface IPronunciationService
    {
        Task<PronunciationAssessmentResult> AssessPronunciationAsync(string audioUrl, string referenceText, string languageCode);
        AssessmentResult ConvertToAssessmentResult(PronunciationAssessmentResult azureResult, string referenceText, string languageCode);
    }
}
