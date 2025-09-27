using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class ExerciseOptionRepository : GenericRepository<ExerciseOption>, IExerciseOptionRepository
    {
        public ExerciseOptionRepository(AppDbContext context) : base(context) { }
    }
}
