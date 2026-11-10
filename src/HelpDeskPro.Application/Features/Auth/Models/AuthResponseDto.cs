namespace HelpDeskPro.Application.Features.Auth.Models;

public record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAt,
    UserDto User);

