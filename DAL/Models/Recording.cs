using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Recording
    {
        [Key]
        public Guid RecordingID { get; set; }

        public string? Url { get; set; }

        [Required]
        public Guid UserID { get; set; }

        public Guid LanguageID { get; set; }
        public Language Language { get; set; }

        [Required]
        [StringLength(300)]
        public string FilePath { get; set; }



        [Required]
        public DateTime CreatedAt { get; set; }
        public DateTime? Duration { get; set; }
        public Guid ConverationID { get; set; }
        public string Format { get; set; }
    }
}
