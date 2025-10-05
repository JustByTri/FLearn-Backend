using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Settings
{
    public class RedisSettings
    {
        public string ConnectionString { get; set; } = "localhost:6379";
        public string Password { get; set; } = "";
        public string InstanceName { get; set; } = "FLearnApp_";
        public int VoiceAssessmentExpiryMinutes { get; set; } = 180; // 3 hours
        public int ResultExpiryDays { get; set; } = 30; // 30 days
    }
}
