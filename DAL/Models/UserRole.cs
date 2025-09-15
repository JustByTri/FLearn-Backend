using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

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
    }
}
