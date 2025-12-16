namespace GearUp.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; } = default!;
        public string SuccessMessage { get; private set; } = string.Empty;
        public string ErrorMessage { get; private set; } =  string.Empty;
        public int Status { get; set; }

        public static Result<T> Success(T data, string successMessage = "Success", int status = 200) => new Result<T>
        {
            IsSuccess = true,
            Data = data,
            SuccessMessage = successMessage,
            Status = status,
        };


       public static Result<T> Failure(string errorMessage = "Internal error occured", int status = 500) => new Result<T>
        {
            IsSuccess = false,
            Data = default!,
            ErrorMessage = errorMessage,
            Status = status
        };
    }
}
