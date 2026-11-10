namespace HelpDeskPro.Application.Features.Tickets.Commands.ChangeTicketStatus;

using FluentValidation;
using HelpDeskPro.Domain.Enums;

public class ChangeTicketStatusCommandValidator : AbstractValidator<ChangeTicketStatusCommand>
{
    public ChangeTicketStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID tiket tidak valid.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status tiket tidak valid.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Alasan penundaan tiket wajib diisi.")
            .When(x => x.Status == TicketStatus.Pending);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Catatan penyelesaian tiket wajib diisi.")
            .When(x => x.Status == TicketStatus.Resolved);

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion wajib disertakan.");
    }
}

