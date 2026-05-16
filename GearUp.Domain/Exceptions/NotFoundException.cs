namespace GearUp.Domain.Exceptions
{
    public class NotFoundException : BaseException
    {
        public NotFoundException(string message) 
            : base(message, 404)
        {
        }

        public NotFoundException(string name, object key)
            : base($"Entity \"{name}\" ({key}) was not found.", 404)
        {
        }
    }
}
