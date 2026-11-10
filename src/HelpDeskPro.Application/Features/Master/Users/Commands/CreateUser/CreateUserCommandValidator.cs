namespace HelpDeskPro.Application.Features.Master.Users.Commands.CreateUser;

using FluentValidation;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private static readonly string[] _validRoles = ["Requester", "Agent", "Supervisor", "Admin"];

    public CreateUserCommandValidator()
    {
        RuleFor(x => x.EmployeeNo)
            .NotEmpty().WithMessage("Nomor karyawan (NIK) wajib diisi.")
            .MaximumLength(20).WithMessage("Nomor karyawan maksimal 20 karakter.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nama lengkap wajib diisi.")
            .MaximumLength(150).WithMessage("Nama lengkap maksimal 150 karakter.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.")
            .MaximumLength(256).WithMessage("Email maksimal 256 karakter.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password wajib diisi.")
            .MinimumLength(6).WithMessage("Password minimal 6 karakter.");

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage("Departemen wajib dipilih.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role wajib diisi.")
            .Must(r => _validRoles.Contains(r))
            .WithMessage("Role harus bernilai 'Requester', 'Agent', 'Supervisor', atau 'Admin'.");
    }
}

