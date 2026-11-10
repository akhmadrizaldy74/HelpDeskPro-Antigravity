namespace HelpDeskPro.Application.Features.Master.Holidays.Commands.CreateHoliday;

using FluentValidation;

public sealed class CreateHolidayCommandValidator : AbstractValidator<CreateHolidayCommand>
{
    public CreateHolidayCommandValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Tanggal libur wajib diisi.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama hari libur wajib diisi.")
            .MaximumLength(100).WithMessage("Nama hari libur maksimal 100 karakter.");
    }
}
