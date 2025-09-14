namespace GearUp.Presentation.DTOs
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Success(T data, string message = "Success") => new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
        };
        
        public static ApiResponse<T> Failure(string message = "Internal error occurred") => new ApiResponse<T>
        {
            IsSuccess = false,
            Data = default,
            Message = message
        };
    }
}
