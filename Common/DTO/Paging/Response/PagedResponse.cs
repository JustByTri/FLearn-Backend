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
        public static PagedResponse<T> Fail(
            object errors,
            string message = "Validation failed",
            int code = 400)
        {
            return new PagedResponse<T>
            {
                Status = "fail",
                Code = code,
                Message = message,
                Errors = errors
            };
        }
        public static PagedResponse<T> Error(
            string message = "Internal server error",
            int code = 500,
            object errors = null)
        {
            return new PagedResponse<T>
            {
                Status = "error",
                Code = code,
                Message = message,
                Errors = errors
            };
        }
    }
}
