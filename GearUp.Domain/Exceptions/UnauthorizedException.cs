namespace GearUp.Domain.Exceptions
{
    public class UnauthorizedException : BaseException
    {
        public UnauthorizedException(string message = "Unauthorized access.") 
            : base(message, 401)
        {
        }
    }
}
