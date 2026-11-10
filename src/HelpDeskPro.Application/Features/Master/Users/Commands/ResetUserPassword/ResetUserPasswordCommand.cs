namespace HelpDeskPro.Application.Features.Master.Users.Commands.ResetUserPassword;

using MediatR;

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest;

