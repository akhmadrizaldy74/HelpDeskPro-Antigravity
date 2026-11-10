namespace HelpDeskPro.Application.Features.Master.SlaPolicies.Commands.UpdateSlaPolicy;

using FluentValidation;

public sealed class UpdateSlaPolicyCommandValidator : AbstractValidator<UpdateSlaPolicyCommand>
{
    public UpdateSlaPolicyCommandValidator()
    {
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Prioritas tiket tidak valid.");

        RuleFor(x => x.ResponseTimeMinutes)
            .GreaterThan(0).WithMessage("Waktu respons harus lebih besar dari 0 menit.");

        RuleFor(x => x.ResolutionTimeMinutes)
            .GreaterThan(0).WithMessage("Waktu resolusi harus lebih besar dari 0 menit.")
            .Must((cmd, res) => res >= cmd.ResponseTimeMinutes)
            .WithMessage("Waktu resolusi harus lebih besar atau sama dengan waktu respons.");
    }
}

