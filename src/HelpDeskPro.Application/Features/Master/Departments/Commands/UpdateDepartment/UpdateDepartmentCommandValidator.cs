namespace HelpDeskPro.Application.Features.Master.Departments.Commands.UpdateDepartment;

using FluentValidation;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID departemen tidak valid.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Kode departemen wajib diisi.")
            .MaximumLength(20).WithMessage("Kode departemen maksimal 20 karakter.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Kode departemen hanya boleh berisi huruf, angka, tanda minus, dan garis bawah.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama departemen wajib diisi.")
            .MaximumLength(100).WithMessage("Nama departemen maksimal 100 karakter.");
    }
}

