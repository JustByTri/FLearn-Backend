using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Role
    {
        [Key]
        public Guid RoleID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<UserRole> UserRoles
        {
            get; set;
        }
    }
}
