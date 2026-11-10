namespace HelpDeskPro.Application.Features.Tickets.Commands.RateTicket;

using FluentValidation;

public class RateTicketCommandValidator : AbstractValidator<RateTicketCommand>
{
    public RateTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .GreaterThan(0)
            .WithMessage("ID tiket harus valid.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating kepuasan harus bernilai antara 1 dan 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500)
            .WithMessage("Komentar ulasan tidak boleh melebihi 500 karakter.");
    }
}
