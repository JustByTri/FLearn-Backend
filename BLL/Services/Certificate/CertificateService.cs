using BLL.IServices.Certificate;
using Common.DTO.Certificate.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Certificate
{
    public class CertificateService : ICertificateService
    {
        private readonly IUnitOfWork _unit;
        public CertificateService(IUnitOfWork unit)
        {
            _unit = unit;
        }
        public async Task<PagedResponse<IEnumerable<CertificateResponse>>> GetCertificatesByLang(string lang, PagingRequest request)
        {
            var query = _unit.CertificateTypes.Query()
                .Include(c => c.Language)
                .Where(c => c.Language.LanguageCode.ToLower() == lang.ToLower().Trim() && c.Status)
                .OrderByDescending(c => c.CreatedAt);

            var totalItems = await query.CountAsync();


            var certificates = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new CertificateResponse
                {
                    CertificateId = c.CertificateTypeId,
                    Name = c.Name,
                    Description = c.Description,
                }).ToListAsync();

            return PagedResponse<IEnumerable<CertificateResponse>>.Success(
                data: certificates,
                page: request.Page,
                pageSize: request.PageSize,
                totalItems: totalItems);
        }
    }
}
