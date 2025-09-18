using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Admin
{
    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalStaff { get; set; }
        public int TotalCourses { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingCourses { get; set; }
        public List<UserListDto> RecentUsers { get; set; } = new List<UserListDto>();
    }
}
