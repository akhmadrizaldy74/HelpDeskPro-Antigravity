# Laporan P-03 — Autentikasi & Otorisasi HelpDeskPro

- **Tanggal**: 9 November 2026
- **Status**: Selesai
- **Target Platform**: .NET 8.0 SDK

---

## 1. Rangkuman Pengerjaan
Telah diimplementasikan modul autentikasi dan otorisasi backend sesuai acuan FSD 3.1 & 4.1 (FR-01, FR-02) serta [.agent/rules/coding-standard.md](file:///c:/Users/Akhmad%20Rizaldy/.gemini/antigravity-ide/scratch/HelpDeskPro-Antigravity/HelpDeskPro-Antigravity/.agent/rules/coding-standard.md):

1. **Endpoint `POST /api/auth/login`**:
   - Menerima `LoginCommand` (Email, Password) tervalidasi via FluentValidation.
   - Memeriksa lockout dan password Identity.
   - Mengembalikan `AuthResponseDto` (`accessToken` berlaku 8 jam, `expiresAt`, dan data `user`: `id`, `fullName`, `email`, `role`, `departmentName`) terbungkus `ApiResponse<T>`.
2. **Konfigurasi Lockout (FR-02)**:
   - Terkunci 15 menit jika 5 kali gagal login berturut-turut.
3. **Endpoint `GET /api/auth/me`**:
   - Mengambil data profil pengguna yang sedang login berdasarkan claims token JWT.
4. **Authorization Policies**:
   - `RequireAgentOrAbove`: Role Agent, Supervisor, Admin.
   - `RequireSupervisorOrAbove`: Role Supervisor, Admin.
   - `RequireAdmin`: Role Admin.
5. **Konfigurasi JWT**:
   - `JwtOptions` (Issuer, Audience, SecretKey, ExpiryHours = 8) dikonfigurasi via Options pattern.
   - Autentikasi `JwtBearer` terdaftar di API pipeline.
6. **Unit Tests (3 skenario baru)**:
   - Login sukses (`Handle_WhenCredentialsAreValid_ShouldReturnTokenAndUserDto`).
   - Password salah (`Handle_WhenPasswordIsIncorrect_ShouldIncrementAccessFailedCountAndThrowBusinessRuleException`).
   - Akun terkunci (`Handle_WhenAccountIsLockedOut_ShouldThrowBusinessRuleException`).
7. **Integration Tests (3 skenario baru)**:
   - Token valid → HTTP 200 OK (`Endpoint_WithValidToken_ShouldReturn200Ok`).
   - Tanpa token → HTTP 401 Unauthorized (`Endpoint_WithoutToken_ShouldReturn401Unauthorized`).
   - Role kurang → HTTP 403 Forbidden (`Endpoint_WithInsufficientRole_ShouldReturn403Forbidden`).

---

## 2. Hasil Verifikasi
- `dotnet build --configuration Release`: **0 Warning(s), 0 Error(s)**
- `dotnet test --configuration Release`: **10/10 tests passed (0 failed)**
  - 7 Unit tests passed.
  - 3 Integration tests passed.

