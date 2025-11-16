namespace Common.DTO.Purchases.Request
{
    public class PurchasePagingRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Status { get; set; }
        public bool? ActiveOnly { get; set; }
    }
}
