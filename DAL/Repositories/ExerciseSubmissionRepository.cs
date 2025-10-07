using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class ExerciseSubmissionRepository : GenericRepository<ExerciseSubmission>, IExerciseSubmissionRepository
    {
        public ExerciseSubmissionRepository(AppDbContext context) : base(context) { }
    }
}
