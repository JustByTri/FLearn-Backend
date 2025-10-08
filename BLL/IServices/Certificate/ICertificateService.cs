using Common.DTO.Certificate.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Certificate
{
    public interface ICertificateService
    {
        Task<PagedResponse<IEnumerable<CertificateResponse>>> GetCertificatesByLang(string lang, PagingRequest request);
    }
}
