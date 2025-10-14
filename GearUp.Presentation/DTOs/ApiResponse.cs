namespace GearUp.Presentation.DTOs
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int Status { get; set; }

        public static ApiResponse<T> Success(T data, string message = "Success", int status = 200) => new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            Status = status
        };

        public static ApiResponse<T> Failure(string message = "Internal error occurred", int status = 500) => new ApiResponse<T>
        {
            IsSuccess = false,
            Data = default,
            Message = message,
            Status = status
        };
    }
}
