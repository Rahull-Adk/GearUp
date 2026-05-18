namespace GearUp.Domain.Exceptions
{
    public abstract class BaseException : Exception
    {
        public int StatusCode { get; }
        public string? Details { get; }

        protected BaseException(string message, int statusCode, string? details = null)
            : base(message)
        {
            StatusCode = statusCode;
            Details = details;
        }
    }
}
