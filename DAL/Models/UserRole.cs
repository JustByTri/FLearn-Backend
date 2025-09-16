using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class UserRole
    {
        [Key]
        public Guid UserRoleID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        [Required]
        public Guid RoleID { get; set; }
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; }
    }
}
