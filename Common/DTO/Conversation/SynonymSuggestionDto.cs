using System;
using System.Collections.Generic;

namespace Common.DTO.Conversation
{
    /// <summary>
    /// DTO cho g?i ý t? ??ng ngh?a theo trình ??
    /// </summary>
    public class SynonymSuggestionDto
    {
        /// <summary>
        /// Câu g?c c?a user
        /// </summary>
        public string OriginalMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// Trình ?? hi?n t?i (A1, A2, B1, B2, C1, C2)
        /// </summary>
        public string CurrentLevel { get; set; } = string.Empty;
        
        /// <summary>
        /// Các cách nói t??ng ???ng ? trình ?? cao h?n
        /// </summary>
        public List<LeveledAlternative> Alternatives { get; set; } = new();
        
        /// <summary>
        /// Gi?i thích t?i sao nên dùng cách nói khác
        /// </summary>
        public string? Explanation { get; set; }
    }
    
    public class LeveledAlternative
    {
        /// <summary>
        /// Trình ?? c?a cách nói này (B1, B2, C1, C2)
        /// </summary>
        public string Level { get; set; } = string.Empty;
        
        /// <summary>
        /// Cách di?n ??t thay th?
        /// </summary>
        public string AlternativeText { get; set; } = string.Empty;
        
        /// <summary>
        /// Gi?i thích ng?n g?n s? khác bi?t
        /// </summary>
        public string? Difference { get; set; }
        
        /// <summary>
        /// Ví d? s? d?ng trong ng? c?nh
        /// </summary>
        public string? ExampleUsage { get; set; }
    }
}
