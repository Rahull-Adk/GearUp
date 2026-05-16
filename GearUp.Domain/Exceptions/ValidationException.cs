namespace GearUp.Domain.Exceptions
{
    public class ValidationException : BaseException
    {
        public ValidationException(string message, string? details = null) 
            : base(message, 422, details)
        {
        }

        public ValidationException(IDictionary<string, string[]> failures)
            : base("One or more validation failures have occurred.", 422, System.Text.Json.JsonSerializer.Serialize(failures))
        {
        }
    }
}
