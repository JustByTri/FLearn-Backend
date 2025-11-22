using Common.DTO.Paging.Request;

namespace Common.DTO.Refund.Request
{
    public class RefundRequestFilterRequest : PagingRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
