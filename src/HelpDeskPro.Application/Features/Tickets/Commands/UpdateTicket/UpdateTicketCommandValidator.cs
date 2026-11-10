namespace HelpDeskPro.Application.Features.Tickets.Commands.UpdateTicket;

using FluentValidation;

public class UpdateTicketCommandValidator : AbstractValidator<UpdateTicketCommand>
{
    public UpdateTicketCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID tiket tidak valid.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Judul tiket tidak boleh kosong.")
            .Length(5, 200).WithMessage("Judul tiket harus antara 5 dan 200 karakter.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Deskripsi tiket melebihi batas maksimal 4000 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Kategori wajib dipilih.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Prioritas tiket tidak valid.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion wajib disertakan.");
    }
}

