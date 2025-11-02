using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class UnitProgressRepository : GenericRepository<UnitProgress>, IUnitProgressRepository
    {
        public UnitProgressRepository(AppDbContext context) : base(context) { }
    }
}
