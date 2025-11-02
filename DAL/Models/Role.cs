using DAL.Helpers;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Role
    {
        [Key]
        public Guid RoleID { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public bool Status { get; set; } = true;
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
