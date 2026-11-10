namespace HelpDeskPro.Infrastructure.Services;

using HelpDeskPro.Application.Common.Interfaces;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

