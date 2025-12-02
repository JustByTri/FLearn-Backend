using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    /// <summary>
    /// Repository cho qu?n lý yêu c?u h?y l?p
    /// </summary>
    public interface IClassCancellationRequestRepository : IGenericRepository<ClassCancellationRequest>
    {
        /// <summary>
        /// L?y danh sách yêu c?u h?y l?p ?ang ch? duy?t theo ngôn ng? (cho Manager)
        /// </summary>
        Task<IEnumerable<ClassCancellationRequest>> GetPendingRequestsByManagerLanguageAsync(Guid languageId);

        /// <summary>
        /// L?y chi ti?t yêu c?u h?y l?p kèm thông tin liên quan
        /// </summary>
        Task<ClassCancellationRequest?> GetByIdWithDetailsAsync(Guid requestId);

        /// <summary>
        /// Ki?m tra xem l?p h?c ?ã có yêu c?u h?y pending ch?a
        /// </summary>
        Task<bool> HasPendingRequestAsync(Guid classId);
    }
}
