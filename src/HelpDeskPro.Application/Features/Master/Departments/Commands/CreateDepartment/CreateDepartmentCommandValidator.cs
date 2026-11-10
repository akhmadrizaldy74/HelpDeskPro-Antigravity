namespace HelpDeskPro.Application.Features.Master.Departments.Commands.CreateDepartment;

using FluentValidation;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Kode departemen wajib diisi.")
            .MaximumLength(20).WithMessage("Kode departemen maksimal 20 karakter.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Kode departemen hanya boleh berisi huruf, angka, tanda minus, dan garis bawah.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama departemen wajib diisi.")
            .MaximumLength(100).WithMessage("Nama departemen maksimal 100 karakter.");
    }
}

