namespace HelpDeskPro.Application.Features.Master.Users.Commands.UpdateUser;

using FluentValidation;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    private static readonly string[] _validRoles = ["Requester", "Agent", "Supervisor", "Admin"];

    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID pengguna wajib diisi.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nama lengkap wajib diisi.")
            .MaximumLength(150).WithMessage("Nama lengkap maksimal 150 karakter.");

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage("Departemen wajib dipilih.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role wajib diisi.")
            .Must(r => _validRoles.Contains(r))
            .WithMessage("Role harus bernilai 'Requester', 'Agent', 'Supervisor', atau 'Admin'.");

        RuleFor(x => x.SupervisorId)
            .Must((cmd, supId) => !supId.HasValue || supId.Value != cmd.Id)
            .WithMessage("Pengguna tidak dapat menjadi supervisor bagi dirinya sendiri.");
    }
}

