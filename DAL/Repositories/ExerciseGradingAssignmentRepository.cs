using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class ExerciseGradingAssignmentRepository : GenericRepository<ExerciseGradingAssignment>, IExerciseGradingAssignmentRepository
    {
        public ExerciseGradingAssignmentRepository(AppDbContext context) : base(context) { }
    }
}
