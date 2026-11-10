namespace HelpDeskPro.Application.Features.Master.Users.Commands.ResetUserPassword;

using FluentValidation;

public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ID pengguna wajib diisi.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password baru wajib diisi.")
            .MinimumLength(6).WithMessage("Password minimal 6 karakter.");
    }
}

