using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class CourseTopic
    {
        [Key]
        public Guid CourseTopicID { get; set; }

        [Required]
        public Guid CourseID { get; set; }

        [Required]
        public Guid TopicID { get; set; }

        public Course? Course { get; set; }
        public Topic? Topic { get; set; }
    }
}
