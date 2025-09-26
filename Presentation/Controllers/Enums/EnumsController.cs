using DAL.Type;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Enums
{
    [Route("api/enums")]
    [ApiController]
    public class EnumsController : ControllerBase
    {
        [HttpGet("course-status")]
        public IActionResult GetCourseStatuses()
        {
            var statuses = Enum.GetValues(typeof(CourseStatus))
                .Cast<CourseStatus>()
                .Select(s => new
                {
                    Id = (int)s,
                    Name = s.ToString()
                });

            return Ok(statuses);
        }

        [HttpGet("level-types")]
        public IActionResult GetLevelTypes()
        {
            var levels = Enum.GetValues(typeof(LevelType))
                .Cast<LevelType>()
                .Select(l => new
                {
                    Id = (int)l,
                    Name = l.ToString()
                });

            return Ok(levels);
        }

        [HttpGet("skill-types")]
        public IActionResult GetSkillTypes()
        {
            var skills = Enum.GetValues(typeof(SkillType))
                .Cast<SkillType>()
                .Select(s => new
                {
                    Id = (int)s,
                    Name = s.ToString()
                });

            return Ok(skills);
        }
    }
}
