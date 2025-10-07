using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class ExerciseRepository : GenericRepository<Exercise>, IExerciseRepository
    {
        public ExerciseRepository(AppDbContext context) : base(context) { }

    }
}
