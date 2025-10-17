using Common.DTO.Conversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Coversation
{
    public interface IConversationPartnerService
    {
        Task<List<ConversationLanguageDto>> GetAvailableLanguagesAsync();
        Task<List<ConversationTopicDto>> GetAvailableTopicsAsync();
        Task<List<string>> GetLevelsByLanguageAsync(Guid languageId);
        Task<ConversationSessionDto> StartConversationAsync(Guid userId, StartConversationRequestDto request);
        Task<ConversationMessageDto> SendMessageAsync(Guid userId, SendMessageRequestDto request);
        Task<ConversationEvaluationDto> EndConversationAsync(Guid userId, Guid sessionId);
        Task<List<ConversationSessionDto>> GetUserConversationHistoryAsync(Guid userId);
        Task<ConversationSessionDto?> GetConversationSessionAsync(Guid userId, Guid sessionId);
        Task<ConversationUsageDto> GetConversationUsageAsync(Guid userId);
    }
}
