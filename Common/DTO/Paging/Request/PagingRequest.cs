namespace Common.DTO.Paging.Request
{

    public class PagingRequest
    {
        public int Page { get; set; } = 1;      // default page 1
        public int PageSize { get; set; } = 10; // default 10 item / page
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        public string? Status { get; set; }
    }
}
