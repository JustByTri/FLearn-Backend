using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class QualificationAssessment
    {
        public string CredentialName { get; set; } = string.Empty;
        public string CredentialType { get; set; } = string.Empty;
        public int RelevanceScore { get; set; }
        public string Assessment { get; set; } = string.Empty;
        public List<string> SupportedLevels { get; set; } = new();
    }
}
