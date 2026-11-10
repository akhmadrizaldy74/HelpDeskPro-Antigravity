namespace HelpDeskPro.Application.Common.Interfaces;

using HelpDeskPro.Domain.Entities;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, IList<string> roles, string? departmentName);
}

