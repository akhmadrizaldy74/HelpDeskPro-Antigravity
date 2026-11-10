namespace HelpDeskPro.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) tidak ditemukan.")
    {
    }
}

