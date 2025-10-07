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
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Status { get; set; } = true; // Active by default or false for inactive
        public virtual ICollection<UserRole>? UserRoles { get; set; } = new List<UserRole>();
    }
}
