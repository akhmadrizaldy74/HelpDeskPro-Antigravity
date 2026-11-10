namespace HelpDeskPro.Application.Features.Tickets.Commands.CreateTicket;

using FluentValidation;

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Judul tiket wajib diisi.")
            .MinimumLength(5).WithMessage("Judul tiket minimal 5 karakter.")
            .MaximumLength(200).WithMessage("Judul tiket maksimal 200 karakter.");

        RuleFor(v => v.Description)
            .MaximumLength(4000).WithMessage("Deskripsi tiket maksimal 4000 karakter.")
            .When(v => !string.IsNullOrEmpty(v.Description));

        RuleFor(v => v.CategoryId)
            .GreaterThan(0).WithMessage("Kategori tiket wajib dipilih.");

        RuleFor(v => v.Priority)
            .IsInEnum().WithMessage("Prioritas tiket tidak valid.");
    }
}

