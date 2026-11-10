namespace HelpDeskPro.Application.Features.Auth.Commands.Login;

using HelpDeskPro.Application.Features.Auth.Models;
using MediatR;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;

