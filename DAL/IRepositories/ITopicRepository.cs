using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface ITopicRepository : IGenericRepository<Topic>
    {
        Task<Topic> GetByNameAsync(string name);
        Task<List<Topic>> SearchTopicsAsync(string searchTerm);
        Task<bool> IsTopicNameExistsAsync(string name);
        Task<List<Topic>> GetTopicsWithCoursesAsync();
    }
}
