namespace CloudNext.DTOs
{
    public class ApiResponse<T>
    {
        public T? Result { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public static ApiResponse<T> SuccessResponse(T result)
        {
            return new ApiResponse<T> { Result = result, Success = true, ErrorMessage = null };
        }

        public static ApiResponse<T> ErrorResponse(string errorMessage)
        {
            return new ApiResponse<T> { Result = default, Success = false, ErrorMessage = errorMessage };
        }
    }
}
