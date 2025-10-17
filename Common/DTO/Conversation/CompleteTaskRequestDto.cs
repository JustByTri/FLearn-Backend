using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class CompleteTaskRequestDto
    {
        public Guid SessionId { get; set; }
        public Guid TaskId { get; set; }
        public string? CompletionNotes { get; set; }
    }
}
