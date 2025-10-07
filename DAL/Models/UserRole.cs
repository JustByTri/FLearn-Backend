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
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
        [Required]
        public Guid RoleID { get; set; }
        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
