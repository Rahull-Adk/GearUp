namespace GearUp.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; } = default!;
        public string SuccessMessage { get; private set; } = string.Empty;
        public string ErrorMessage { get; private set; } =  string.Empty;

        public static Result<T> Success(T data, string successMessage = "Success") => new Result<T>
        {
            IsSuccess = true,
            Data = data,
            SuccessMessage = successMessage,
        };


       public static Result<T> Failure(string errorMessage = "Internal error occured") => new Result<T>
        {
            IsSuccess = false,
            Data = default!,
            ErrorMessage = errorMessage
        };
    }
}
