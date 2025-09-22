using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Settings
{
    public class GeminiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
        public string Model { get; set; } = "gemini-1.5-flash";
        public int MaxTokens { get; set; } = 2048;
        public double Temperature { get; set; } = 0.7;
    }
}
