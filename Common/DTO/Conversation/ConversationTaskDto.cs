using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class ConversationTaskDto
    {
        public Guid TaskId { get; set; }
        public string TaskDescription { get; set; }
        public string? TaskContext { get; set; }
        public int TaskSequence { get; set; }
        public string Status { get; set; }
        public bool IsCompleted { get; set; }
        public string? CompletionNotes { get; set; }
    }
}
