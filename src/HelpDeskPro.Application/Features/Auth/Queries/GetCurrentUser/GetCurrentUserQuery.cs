namespace HelpDeskPro.Application.Features.Auth.Queries.GetCurrentUser;

using HelpDeskPro.Application.Features.Auth.Models;
using MediatR;

public record GetCurrentUserQuery : IRequest<UserDto>;

