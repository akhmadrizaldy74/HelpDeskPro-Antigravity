namespace HelpDeskPro.Domain.Exceptions;

public class BusinessRuleException : DomainException
{
    public BusinessRuleException()
    {
    }

    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

