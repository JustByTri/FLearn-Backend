using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Language.Response
{
    public class LanguageLevelDto
    {
        public Guid LanguageLevelID { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
    }
}
