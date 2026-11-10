namespace HelpDeskPro.Application.Features.Tickets.Commands.AddComment;

using FluentValidation;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .GreaterThan(0).WithMessage("ID tiket tidak valid.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Isi komentar tidak boleh kosong.")
            .MaximumLength(4000).WithMessage("Isi komentar melebihi batas maksimal 4000 karakter.");
    }
}

