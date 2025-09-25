namespace Common.DTO.ApiResponse
{
    public class BaseResponse<T>
    {
        public string Status { get; set; } // success | fail | error
        public int Code { get; set; }      // HTTP status code
        public string Message { get; set; }
        public T Data { get; set; }
        public object Errors { get; set; }
        public object Meta { get; set; }

        public static BaseResponse<T> Success(T data, string message = "Success", int code = 200)
        {
            return new BaseResponse<T>
            {
                Status = "success",
                Code = code,
                Message = message,
                Data = data
            };
        }

        public static BaseResponse<T> Fail(object errors, string message = "Validation failed", int code = 400)
        {
            return new BaseResponse<T>
            {
                Status = "fail",
                Code = code,
                Message = message,
                Errors = errors
            };
        }

        public static BaseResponse<T> Error(string message = "Internal server error", int code = 500, object errors = null)
        {
            return new BaseResponse<T>
            {
                Status = "error",
                Code = code,
                Message = message,
                Errors = errors
            };
        }
    }
}
