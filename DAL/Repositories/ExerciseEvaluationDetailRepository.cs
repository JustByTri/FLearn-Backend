using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class ExerciseEvaluationDetailRepository : GenericRepository<ExerciseEvaluationDetail>, IExerciseEvaluationDetailRepository
    {
        public ExerciseEvaluationDetailRepository(AppDbContext context) : base(context) { }
    }
}
