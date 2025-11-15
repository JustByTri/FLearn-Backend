using BLL.IServices.Gamification;
using Common.DTO.ApiResponse;
using Common.DTO.Gamification;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Presentation.Controllers.Learner
{
    [Route("api/gamification")] 
    [ApiController]
    [Authorize(Roles = "Learner")] // all endpoints require learner
    public class GamificationController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamificationService _gamificationService;
        public GamificationController(IUnitOfWork unitOfWork, IGamificationService gamificationService)
        {
            _unitOfWork = unitOfWork;
            _gamificationService = gamificationService;
        }

        /// <summary>
        /// Lấy trạng thái XP hiện tại cho ngôn ngữ đang hoạt động (hoặc ngôn ngữ đầu tiên nếu chưa chọn).
        /// </summary>
        [HttpGet("me/status")]
        public async Task<IActionResult> GetMyXpStatus()
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;

            // tìm learner language theo active language trước
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            var allLearner = await _unitOfWork.LearnerLanguages.GetAllAsync();
            var learner = allLearner
                .FirstOrDefault(l => l.UserId == userId && (user?.ActiveLanguageId == null || l.LanguageId == user!.ActiveLanguageId))
                ?? allLearner.FirstOrDefault(l => l.UserId == userId);

            if (learner == null)
                return NotFound(BaseResponse<object>.Fail("Learner language not found."));

            await _gamificationService.EnsureDailyXpResetAsync(learner);

            var dto = new XpStatusDto
            {
                LearnerLanguageId = learner.LearnerLanguageId,
                LanguageId = learner.LanguageId,
                ExperiencePoints = learner.ExperiencePoints,
                TodayXp = learner.TodayXp,
                DailyXpGoal = learner.DailyXpGoal,
                LastXpResetDate = learner.LastXpResetDate,
                StreakDays = learner.StreakDays,
                Level = _gamificationService.GetLevelFromXp(learner.ExperiencePoints),
                LevelProgress = _gamificationService.GetLevelProgress(learner.ExperiencePoints)
            };
            return Ok(BaseResponse<XpStatusDto>.Success(dto, "XP status retrieved"));
        }

        /// <summary>
        /// Cộng XP thủ công (exercise/conversation...).
        /// </summary>
        [HttpPost("me/xp/increment")]
        public async Task<IActionResult> IncrementXp([FromBody] IncrementXpRequestDto request)
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;
            if (!ModelState.IsValid) return BadRequest(BaseResponse<object>.Fail("Invalid data"));

            var learner = (await _unitOfWork.LearnerLanguages.GetAllAsync()).FirstOrDefault(l => l.UserId == userId);
            if (learner == null) return NotFound(BaseResponse<object>.Fail("Learner not found"));

            var (total, today, level) = await _gamificationService.AwardXpAsync(learner, request.Amount, request.Source);
            var dto = new XpStatusDto
            {
                LearnerLanguageId = learner.LearnerLanguageId,
                LanguageId = learner.LanguageId,
                ExperiencePoints = total,
                TodayXp = today,
                DailyXpGoal = learner.DailyXpGoal,
                LastXpResetDate = learner.LastXpResetDate,
                StreakDays = learner.StreakDays,
                Level = level,
                LevelProgress = _gamificationService.GetLevelProgress(total)
            };
            return Ok(BaseResponse<XpStatusDto>.Success(dto, "XP updated"));
        }

        /// <summary>
        /// Cập nhật mục tiêu XP mỗi ngày.
        /// </summary>
        [HttpPut("me/daily-goal")]
        public async Task<IActionResult> UpdateDailyGoal([FromBody] UpdateDailyGoalDto request)
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;
            if (!ModelState.IsValid) return BadRequest(BaseResponse<object>.Fail("Invalid data"));

            var learner = (await _unitOfWork.LearnerLanguages.GetAllAsync()).FirstOrDefault(l => l.UserId == userId);
            if (learner == null) return NotFound(BaseResponse<object>.Fail("Learner not found"));

            learner.DailyXpGoal = request.DailyXpGoal;
            learner.UpdatedAt = DAL.Helpers.TimeHelper.GetVietnamTime();
            await _unitOfWork.LearnerLanguages.UpdateAsync(learner);
            await _unitOfWork.SaveChangesAsync();

            return Ok(BaseResponse<object>.Success(new { learner.DailyXpGoal }, "Daily goal updated"));
        }

        /// <summary>
        /// Lấy leaderboard theo ngôn ngữ dựa trên tổng XP (fallback streak nếu chưa có XP đủ khác biệt).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("leaderboard/{languageId:guid}")]
        public async Task<IActionResult> GetLeaderboard([FromRoute] Guid languageId, [FromQuery][Range(1,100)] int count = 20)
        {
            var list = (await _unitOfWork.LearnerLanguages.GetAllAsync())
                .Where(l => l.LanguageId == languageId && l.User.Status == true)
                .OrderByDescending(l => l.ExperiencePoints)
                .ThenByDescending(l => l.StreakDays)
                .Take(count)
                .Select((l, idx) => new {
                    rank = idx + 1,
                    userId = l.UserId,
                    userName = l.User.FullName ?? l.User.UserName,
                    avatar = l.User.Avatar,
                    experiencePoints = l.ExperiencePoints,
                    streakDays = l.StreakDays,
                    level = _gamificationService.GetLevelFromXp(l.ExperiencePoints)
                }).ToList();

            return Ok(BaseResponse<object>.Success(list, "Leaderboard retrieved"));
        }

        /// <summary>
        /// Lấy thứ hạng của chính mình theo ngôn ngữ.
        /// </summary>
        [HttpGet("leaderboard/{languageId:guid}/me")]
        public async Task<IActionResult> GetMyRank([FromRoute] Guid languageId)
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;
            var all = (await _unitOfWork.LearnerLanguages.GetAllAsync())
                .Where(l => l.LanguageId == languageId && l.User.Status == true)
                .OrderByDescending(l => l.ExperiencePoints)
                .ThenByDescending(l => l.StreakDays)
                .ToList();
            var me = all.FirstOrDefault(l => l.UserId == userId);
            if (me == null) return NotFound(BaseResponse<object>.Fail("Learner not found"));
            var rank = all.FindIndex(l => l.LearnerLanguageId == me.LearnerLanguageId) + 1;
            var response = new {
                rank,
                experiencePoints = me.ExperiencePoints,
                streakDays = me.StreakDays,
                level = _gamificationService.GetLevelFromXp(me.ExperiencePoints)
            };
            return Ok(BaseResponse<object>.Success(response, "Rank retrieved"));
        }

        /// <summary>XP theo từng ngày trong tuần hiện tại (UTC+7).</summary>
        [HttpGet("me/xp/week")]
        public async Task<IActionResult> GetWeekXp()
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;
            var learner = (await _unitOfWork.LearnerLanguages.GetAllAsync()).FirstOrDefault(l => l.UserId == userId);
            if (learner == null) return NotFound(BaseResponse<object>.Fail("Learner not found"));
            var now = DAL.Helpers.TimeHelper.GetVietnamTime();
            var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
            var dict = await _gamificationService.GetDailyXpAsync(learner.LearnerLanguageId, startOfWeek, endOfWeek);
            var days = Enumerable.Range(0,7).Select(i => startOfWeek.AddDays(i)).Select(d => new { date = d.ToString("yyyy-MM-dd"), xp = dict.ContainsKey(d) ? dict[d] : 0 });
            return Ok(BaseResponse<object>.Success(days, "Weekly XP"));
        }
        /// <summary>XP theo từng ngày trong tháng hiện tại.</summary>
        [HttpGet("me/xp/month")]
        public async Task<IActionResult> GetMonthXp()
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;
            var learner = (await _unitOfWork.LearnerLanguages.GetAllAsync()).FirstOrDefault(l => l.UserId == userId);
            if (learner == null) return NotFound(BaseResponse<object>.Fail("Learner not found"));
            var now = DAL.Helpers.TimeHelper.GetVietnamTime();
            var start = new DateTime(now.Year, now.Month, 1);
            var end = start.AddMonths(1).AddSeconds(-1);
            var dict = await _gamificationService.GetDailyXpAsync(learner.LearnerLanguageId, start, end);
            var days = Enumerable.Range(0, (end - start).Days + 1).Select(i => start.AddDays(i)).Select(d => new { date = d.ToString("yyyy-MM-dd"), xp = dict.ContainsKey(d) ? dict[d] : 0 });
            return Ok(BaseResponse<object>.Success(days, "Monthly XP"));
        }
    }
}
