namespace HelpDeskPro.Application.Features.Tickets.Commands.AssignTicket;

using FluentValidation;

public class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID tiket tidak valid.");

        RuleFor(x => x.AssigneeId)
            .NotEmpty().WithMessage("Assignee ID wajib diisi.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion wajib disertakan.");
    }
}

