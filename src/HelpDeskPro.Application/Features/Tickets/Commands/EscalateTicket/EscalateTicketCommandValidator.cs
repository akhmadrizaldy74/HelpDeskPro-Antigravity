namespace HelpDeskPro.Application.Features.Tickets.Commands.EscalateTicket;

using FluentValidation;

public class EscalateTicketCommandValidator : AbstractValidator<EscalateTicketCommand>
{
    public EscalateTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .GreaterThan(0).WithMessage("ID tiket tidak valid.");

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Alasan eskalasi wajib diisi.")
            .MinimumLength(10).WithMessage("Alasan eskalasi minimal 10 karakter.")
            .MaximumLength(1000).WithMessage("Alasan eskalasi maksimal 1000 karakter.");
    }
}

