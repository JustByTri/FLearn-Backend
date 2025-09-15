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

        public ICollection<User> Users { get; set; }

        [Required]
        public DateTime CreateAt { get; set; }
    }
}
