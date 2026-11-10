namespace HelpDeskPro.Application.Features.Master.Holidays.Commands.UpdateHoliday;

using FluentValidation;

public sealed class UpdateHolidayCommandValidator : AbstractValidator<UpdateHolidayCommand>
{
    public UpdateHolidayCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID hari libur tidak valid.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Tanggal libur wajib diisi.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama hari libur wajib diisi.")
            .MaximumLength(100).WithMessage("Nama hari libur maksimal 100 karakter.");
    }
}
