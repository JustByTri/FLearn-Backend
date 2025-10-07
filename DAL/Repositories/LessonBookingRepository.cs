using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LessonBookingRepository : GenericRepository<LessonBooking>, ILessonBookingRepository
    {
        public LessonBookingRepository(AppDbContext context) : base(context) { }
    }
}
