namespace HelpDeskPro.Application.Common.Interfaces;

public interface ITicketNumberGenerator
{
    Task<string> GenerateTicketNumberAsync(CancellationToken cancellationToken = default);
}

