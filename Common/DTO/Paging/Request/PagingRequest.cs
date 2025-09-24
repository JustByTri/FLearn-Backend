namespace Common.DTO.Paging.Request
{

    public class PagingRequest
    {
        public int Page { get; set; } = 1;      // default page 1
        public int PageSize { get; set; } = 10; // default 10 item / page
    }
}
