namespace HelpDeskPro.Application.Features.Tickets.Commands.UploadTicketAttachment;

using FluentValidation;

public class UploadTicketAttachmentCommandValidator : AbstractValidator<UploadTicketAttachmentCommand>
{
    private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".xlsx", ".txt", ".log"
    };

    public UploadTicketAttachmentCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .GreaterThan(0).WithMessage("ID tiket tidak valid.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Nama file tidak boleh kosong.")
            .Must(BeAnAllowedExtension).WithMessage("Tipe file tidak diizinkan.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("File tidak boleh kosong.")
            .LessThanOrEqualTo(5 * 1024 * 1024).WithMessage("Ukuran file melebihi batas maksimal 5 MB.");
    }

    private static bool BeAnAllowedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && _allowedExtensions.Contains(ext);
    }
}

