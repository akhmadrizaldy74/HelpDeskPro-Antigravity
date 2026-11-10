namespace HelpDeskPro.Application.Features.Auth.Commands.Login;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Auth.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.ToUpperInvariant();

        var user = await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleException("Email atau password yang Anda masukkan salah.");
        }

        if (!user.IsActive)
        {
            throw new BusinessRuleException("Akun Anda telah dinonaktifkan. Silakan hubungi administrator.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new BusinessRuleException("Akun Anda sedang terkunci karena 5 kali percobaan gagal login. Silakan coba lagi setelah 15 menit.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new BusinessRuleException("Akun Anda telah dikunci selama 15 menit karena 5 kali gagal login berturut-turut.");
            }

            throw new BusinessRuleException("Email atau password yang Anda masukkan salah.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Requester";

        var (token, expiresAt) = _tokenService.GenerateToken(user, roles, user.Department?.Name);

        var userDto = new UserDto(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            primaryRole,
            user.Department?.Name);

        return new AuthResponseDto(token, expiresAt, userDto);
    }
}
