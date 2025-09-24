using Common.DTO.ApiResponse;

namespace Common.DTO.Paging.Response
{
    public class PagedResponse<T> : BaseResponse<T>
    {
        public PagingMeta Meta { get; set; } = new PagingMeta();
        public static PagedResponse<T> Success(
            T data,
            int page,
            int pageSize,
            int totalItems,
            string message = "Success",
            int code = 200)
        {
            return new PagedResponse<T>
            {
                Status = "success",
                Code = code,
                Message = message,
                Data = data,
                Meta = new PagingMeta
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                }
            };
        }
    }
}
