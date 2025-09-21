using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Language
    {
        [Key]
        public Guid LanguageID { get; set; }

        [Required]
        [StringLength(100)]
        public string LanguageName { get; set; }

        [Required]
        [StringLength(10)]
        public string LanguageCode { get; set; }

        [Required]
        public DateTime CreateAt { get; set; }

     public virtual ICollection<TeacherApplication> TeacherApplications { get; set; }
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<Roadmap> Roadmaps { get; set; }
        public virtual ICollection<Course> Courses { get; set; }
        public virtual ICollection<Conversation> Conversations { get; set; }
    }
}
