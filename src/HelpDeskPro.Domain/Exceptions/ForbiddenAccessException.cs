namespace HelpDeskPro.Domain.Exceptions;

public class ForbiddenAccessException : DomainException
{
    public ForbiddenAccessException() : base("Anda tidak memiliki akses ke sumber daya ini.")
    {
    }

    public ForbiddenAccessException(string message) : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

