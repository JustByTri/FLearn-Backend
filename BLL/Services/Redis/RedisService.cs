using BLL.IServices.Redis;
using BLL.Settings;
using Common.DTO.Assement;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
// Fix: Explicit using for Redis IDatabase
using RedisDatabase = StackExchange.Redis.IDatabase;
using StackExchange.Redis;

namespace BLL.Services.Redis
{
    public class RedisService : IRedisService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _redis;
        private readonly RedisDatabase _database; // Fix: Use explicit alias
        private readonly ILogger<RedisService> _logger;
        private readonly RedisSettings _redisSettings;

        // Cache key prefixes
        private const string VOICE_ASSESSMENT_PREFIX = "voice_assessment:";
        private const string VOICE_RESULT_PREFIX = "voice_result:";
        private const string USER_ASSESSMENTS_PREFIX = "user_assessments:";

        public RedisService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer redis,
            ILogger<RedisService> logger,
            IOptions<RedisSettings> redisSettings)
        {
            _distributedCache = distributedCache;
            _redis = redis;
            _database = redis.GetDatabase(); // Fix: This now clearly uses StackExchange.Redis
            _logger = logger;
            _redisSettings = redisSettings.Value;
        }

        public async Task<VoiceAssessmentDto?> GetVoiceAssessmentAsync(Guid assessmentId)
        {
            try
            {
                var key = $"{VOICE_ASSESSMENT_PREFIX}{assessmentId}";
                var cachedValue = await _distributedCache.GetStringAsync(key);

                if (string.IsNullOrEmpty(cachedValue))
                    return null;

                var assessment = JsonSerializer.Deserialize<VoiceAssessmentDto>(cachedValue);
                _logger.LogDebug("Retrieved assessment {AssessmentId} from Redis", assessmentId);
                return assessment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessment {AssessmentId} from Redis", assessmentId);
                return null;
            }
        }
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _database.StringSetAsync(key, json, expiry);

                _logger.LogInformation("✅ Set key '{Key}' in Redis with expiry {Expiry}",
                    key, expiry?.ToString() ?? "none");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error setting key '{Key}' in Redis", key);
                throw;
            }
        }
        public async Task SetVoiceAssessmentAsync(VoiceAssessmentDto assessment)
        {
            try
            {
                var key = $"{VOICE_ASSESSMENT_PREFIX}{assessment.AssessmentId}";
                var jsonValue = JsonSerializer.Serialize(assessment);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_redisSettings.VoiceAssessmentExpiryMinutes)
                };

                await _distributedCache.SetStringAsync(key, jsonValue, options);

                // Add to user assessments index
                await AddToUserAssessmentsIndexAsync(assessment.UserId, assessment.AssessmentId, assessment.LanguageId);

                _logger.LogInformation("✅ Saved assessment {AssessmentId} to Redis with {Minutes} minutes expiry",
                    assessment.AssessmentId, _redisSettings.VoiceAssessmentExpiryMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving assessment {AssessmentId} to Redis", assessment.AssessmentId);
                throw;
            }
        }

        public async Task<bool> DeleteVoiceAssessmentAsync(Guid assessmentId)
        {
            try
            {
                var assessment = await GetVoiceAssessmentAsync(assessmentId);
                if (assessment != null)
                {
                    await RemoveFromUserAssessmentsIndexAsync(assessment.UserId, assessmentId);
                }

                var key = $"{VOICE_ASSESSMENT_PREFIX}{assessmentId}";
                await _distributedCache.RemoveAsync(key);

                _logger.LogInformation("Deleted assessment {AssessmentId} from Redis", assessmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assessment {AssessmentId} from Redis", assessmentId);
                return false;
            }
        }

        public async Task<List<VoiceAssessmentDto>> GetActiveVoiceAssessmentsAsync()
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: $"{VOICE_ASSESSMENT_PREFIX}*");

                var assessments = new List<VoiceAssessmentDto>();

                foreach (var key in keys)
                {
                    var cachedValue = await _distributedCache.GetStringAsync(key);
                    if (!string.IsNullOrEmpty(cachedValue))
                    {
                        var assessment = JsonSerializer.Deserialize<VoiceAssessmentDto>(cachedValue);
                        if (assessment != null)
                        {
                            assessments.Add(assessment);
                        }
                    }
                }

                _logger.LogDebug("Retrieved {Count} active assessments from Redis", assessments.Count);
                return assessments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active assessments from Redis");
                return new List<VoiceAssessmentDto>();
            }
        }

        public async Task<List<VoiceAssessmentDto>> GetUserAssessmentsAsync(Guid userId, Guid? languageId = null)
        {
            try
            {
                var userIndexKey = $"{USER_ASSESSMENTS_PREFIX}{userId}";
                var assessmentIds = await _database.SetMembersAsync(userIndexKey);

                var assessments = new List<VoiceAssessmentDto>();

                foreach (var assessmentIdRedisValue in assessmentIds)
                {
                    if (Guid.TryParse(assessmentIdRedisValue, out var assessmentId))
                    {
                        var assessment = await GetVoiceAssessmentAsync(assessmentId);
                        if (assessment != null)
                        {
                            if (!languageId.HasValue || assessment.LanguageId == languageId.Value)
                            {
                                assessments.Add(assessment);
                            }
                        }
                    }
                }

                _logger.LogDebug("Retrieved {Count} assessments for user {UserId}", assessments.Count, userId);
                return assessments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user assessments for {UserId}", userId);
                return new List<VoiceAssessmentDto>();
            }
        }
        public async Task DeleteVoiceAssessmentResultAsync(Guid userId, Guid languageId)
        {
            try
            {
                var key = $"voice_assessment_result:{userId}:{languageId}";
                await _database.KeyDeleteAsync(key);
                _logger.LogInformation("🗑️ Deleted voice assessment result: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting voice assessment result for user {UserId}, language {LanguageId}",
                    userId, languageId);
                throw;
            }
        }

        public async Task DeleteAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
                _logger.LogDebug("🗑️ Deleted Redis key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Redis key: {Key}", key);
                throw;
            }
        }
        public async Task<VoiceAssessmentResultDto?> GetVoiceAssessmentResultAsync(Guid userId, Guid languageId)
        {
            try
            {
                var key = $"{VOICE_RESULT_PREFIX}{userId}_{languageId}";
                var cachedValue = await _distributedCache.GetStringAsync(key);

                if (string.IsNullOrEmpty(cachedValue))
                    return null;

                return JsonSerializer.Deserialize<VoiceAssessmentResultDto>(cachedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessment result for user {UserId}", userId);
                return null;
            }
        }

        public async Task SetVoiceAssessmentResultAsync(Guid userId, Guid languageId, VoiceAssessmentResultDto result)
        {
            try
            {
                var key = $"{VOICE_RESULT_PREFIX}{userId}_{languageId}";
                var jsonValue = JsonSerializer.Serialize(result);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_redisSettings.ResultExpiryDays)
                };

                await _distributedCache.SetStringAsync(key, jsonValue, options);

                _logger.LogInformation("✅ Saved assessment result for user {UserId}, language {LanguageId}", userId, languageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving assessment result for user {UserId}", userId);
                throw;
            }
        }

        private async Task AddToUserAssessmentsIndexAsync(Guid userId, Guid assessmentId, Guid languageId)
        {
            try
            {
                var userIndexKey = $"{USER_ASSESSMENTS_PREFIX}{userId}";
                await _database.SetAddAsync(userIndexKey, assessmentId.ToString());
                await _database.KeyExpireAsync(userIndexKey, TimeSpan.FromMinutes(_redisSettings.VoiceAssessmentExpiryMinutes + 60));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to user assessments index for {UserId}", userId);
            }
        }

        private async Task RemoveFromUserAssessmentsIndexAsync(Guid userId, Guid assessmentId)
        {
            try
            {
                var userIndexKey = $"{USER_ASSESSMENTS_PREFIX}{userId}";
                await _database.SetRemoveAsync(userIndexKey, assessmentId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from user assessments index for {UserId}", userId);
            }
        }

        public async Task<int> ClearAllAssessmentsAsync()
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var assessmentKeys = server.Keys(pattern: $"{VOICE_ASSESSMENT_PREFIX}*");
                var resultKeys = server.Keys(pattern: $"{VOICE_RESULT_PREFIX}*");
                var userKeys = server.Keys(pattern: $"{USER_ASSESSMENTS_PREFIX}*");

                var allKeys = assessmentKeys.Concat(resultKeys).Concat(userKeys).ToList();

                foreach (var key in allKeys)
                {
                    await _distributedCache.RemoveAsync(key);
                }

                _logger.LogInformation("Cleared {Count} Redis keys", allKeys.Count());
                return allKeys.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all assessments from Redis");
                return 0;
            }

        }

    }
}

