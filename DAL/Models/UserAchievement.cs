using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class UserAchievement
    {
        [Key]
        public Guid UserAchievementID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        [Required]
        public Guid AchievementID { get; set; }

        [Required]
        public DateTime AchievedAt { get; set; }
    }
}
