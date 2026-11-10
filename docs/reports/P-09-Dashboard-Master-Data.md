# Laporan Implementasi — P-09 Dashboard & Master Data

| Item | Isi |
|---|---|
| Modul | Dashboard Supervisor & Admin, Master Data Management (FR-30 s.d. FR-34) |
| Tanggal | 10 November 2026 |
| Target Framework | .NET 8 |
| Status | Selesai & Lulus Pengujian (310/310 Tests Passed) |

---

## 1. Ringkasan Implementasi

Pada tahap **P-09 · Dashboard & Master Data**, kami telah mengimplementasikan seluruh kebutuhan metrik agregasi dashboard supervisor & admin, tabel beban kerja agent, antrean eskalasi yang belum ditindaklanjuti, serta pengelolaan master data (Departemen, Kategori, User, SLA Policy) dengan penegakan aturan integritas data (*no hard delete on referenced data*) sesuai spesifikasi FSD 3.1, 4.10, dan 4.11 (FR-30 s.d. FR-34):

1. **Dashboard Supervisor & Admin (FR-30 s.d. FR-32)**:
   - **`GetDashboardSummaryQuery` (FR-30)**:
     - Menerima parameter periode: 7 atau 30 hari (divalidasi oleh `GetDashboardSummaryQueryValidator`).
     - Menghitung jumlah per status tiket (`Open`, `InProgress`, `Pending`, `Resolved`, `Closed`, `Cancelled`, `Total`).
     - Menghitung jumlah tiket SLA lewat (`IsSlaBreached == true` atau melewati `ResolutionDueAt` pada status aktif).
     - Menghitung rata-rata waktu respons dalam menit (`FirstRespondedAt - CreatedAt`).
     - Menghitung rata-rata waktu resolusi dalam menit (`(ResolvedAt - CreatedAt) - TotalPausedMinutes`).
     - **Efisiensi SQL**: Agregasi dilakukan langsung di SQL Server melalui EF Core expression (`GroupBy(t => t.Status)`, `CountAsync`, `AverageAsync`), sehingga tidak ada entitas tiket mentah yang ditarik ke memori aplikasi.
   - **`GetAgentWorkloadQuery` (FR-31)**:
     - Mengambil seluruh staf Agent yang aktif.
     - Menghitung beban kerja tiket aktif (`Open`, `InProgress`, `Pending`) dan tiket yang melanggar SLA per Agent menggunakan SQL aggregation grouping berdasarkan `AssigneeId`.
   - **`GetPendingEscalationsQuery` (FR-32)**:
     - Mengembalikan daftar eskalasi tiket yang belum di-acknowledge (`AcknowledgedAt == null`), diurutkan kronologis (`EscalatedAt` ascending).
   - **Scoping Berbasis Role**:
     - Supervisor dibatasi ke tiket di departemennya (supervisor departemen IT memiliki visibilitas atas seluruh tiket operasional IT).
     - Admin memiliki visibilitas global (seluruh departemen).
     - Role Requester dan Agent ditolak dengan `ForbiddenAccessException` (403 Forbidden).

2. **Master Data Management — Admin Only (FR-33, FR-34)**:
   - **Departemen (`/api/master/departments`)**:
     - Query: `GetDepartmentsQuery` (filter aktif/nonaktif), `GetDepartmentByIdQuery`.
     - Command: `CreateDepartmentCommand`, `UpdateDepartmentCommand`, `ToggleDepartmentStatusCommand`.
     - `DeleteDepartmentCommand`: Penegakan aturan integritas referensial. Jika departemen masih digunakan oleh pengguna atau tiket, sistem menolak penghapusan fisik dengan `BusinessRuleException` (*"Departemen masih digunakan oleh pengguna atau tiket dan tidak dapat dihapus. Silakan nonaktifkan departemen ini."*).
   - **Kategori (`/api/master/categories`)**:
     - Query: `GetCategoriesQuery`, `GetCategoryByIdQuery`.
     - Command: `CreateCategoryCommand`, `UpdateCategoryCommand`, `ToggleCategoryStatusCommand`.
     - `DeleteCategoryCommand`: Jika kategori masih memiliki keterkaitan tiket, sistem menolak dengan `BusinessRuleException` (*"Kategori masih digunakan oleh tiket dan tidak dapat dihapus. Silakan nonaktifkan kategori ini."*).
   - **User Management (`/api/master/users`)**:
     - Model: `ApplicationUser` diperluas dengan kolom `IsActive` (default `true`) melalui migration EF Core `AddIsActiveToUsers`.
     - Query: `GetUsersQuery` (paging, filter role, departemen, active, pencarian teks), `GetUserByIdQuery`.
     - Command: `CreateUserCommand` (NIK & Email unik, inisialisasi password, penugasan role), `UpdateUserCommand` (pembaruan role, departemen, dan supervisor; validasi pencegahan supervisor menunjuk diri sendiri), `ResetUserPasswordCommand`, `ToggleUserStatusCommand` (mengubah `IsActive` dan mengeset `LockoutEnd = DateTimeOffset.MaxValue` saat dinonaktifkan).
     - `DeleteUserCommand`: Jika user memiliki tiket (sebagai pemohon maupun teknisi), komentar, riwayat audit, subordinat, atau eskalasi, sistem menolak penghapusan fisik (*"Pengguna masih memiliki riwayat data terkait dan tidak dapat dihapus. Silakan nonaktifkan pengguna ini."*).
   - **SLA Policy (`/api/master/sla-policies` — FR-34)**:
     - Query: `GetSlaPoliciesQuery` menampilkan kebijakan SLA untuk prioritas Low, Medium, High, dan Critical.
     - Command: `UpdateSlaPolicyCommand` (validasi: waktu resolusi $\ge$ waktu respons, nilai $> 0$).
     - **Dampak Perubahan SLA**: Karena `Ticket.Create` mengevaluasi dan menyimpan waktu jatuh tempo statis (`ResponseDueAt` & `ResolutionDueAt`) pada saat tiket pertama kali dibuat, perubahan nilai SLA hanya berdampak pada tiket baru yang dibuat setelah pembaruan tanpa mengganggu tiket lama yang sedang berjalan.

3. **API Controllers**:
   - `DashboardController`: `[Authorize(Policy = "RequireSupervisorOrAbove")]`, route `/api/dashboard`.
   - `MasterDepartmentsController`: `[Authorize(Policy = "RequireAdmin")]`, route `/api/master/departments`.
   - `MasterCategoriesController`: `[Authorize(Policy = "RequireAdmin")]`, route `/api/master/categories`.
   - `MasterUsersController`: `[Authorize(Policy = "RequireAdmin")]`, route `/api/master/users`.
   - `MasterSlaPoliciesController`: `[Authorize(Policy = "RequireAdmin")]`, route `/api/master/sla-policies`.

---

## 2. Hasil Pengujian & Verifikasi Kualitas

### A. Rekapitulasi Test Execution
- **Unit Tests**: **275 / 275 passed** (100% lulus, 0 failed, 0 skipped).
- **Integration Tests**: **35 / 35 passed** (100% lulus, 0 failed, 0 skipped).
- **Total Tests**: **310 / 310 passed**.

### B. Cakupan Kode (Code Coverage)
Berdasarkan laporan *Coverlet XPlat Code Coverage*:
- **`HelpDeskPro.Application`**: **77.83%** line coverage (Standar FSD: $\ge 70\%$).
- **`HelpDeskPro.Domain`**: **91.54%** line coverage (Standar FSD: $\ge 70\%$).
- **Kepatuhan Warning**: **0 Warnings, 0 Errors** pada kompilasi Release (`TreatWarningsAsErrors=true`).

---

## 3. Daftar Berkas Baru dan Dimodifikasi

| No | Berkas | Status | Keterangan |
|---|---|---|---|
| 1 | `src/HelpDeskPro.Domain/Entities/ApplicationUser.cs` | Modifikasi | Penambahan properti `IsActive` |
| 2 | `src/HelpDeskPro.Infrastructure/Persistence/Configurations/ApplicationUserConfiguration.cs` | Modifikasi | Konfigurasi default value `IsActive = true` |
| 3 | `src/HelpDeskPro.Infrastructure/Persistence/Migrations/20261109174605_AddIsActiveToUsers.cs` | Baru | Migration penambahan kolom `IsActive` ke tabel `Users` |
| 4 | `src/HelpDeskPro.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs` | Modifikasi | Validasi penolakan login untuk akun nonaktif |
| 5 | `src/HelpDeskPro.Application/Features/Dashboard/Models/*` | Baru | DTO `DashboardSummaryDto`, `AgentWorkloadDto`, `PendingEscalationDto` |
| 6 | `src/HelpDeskPro.Application/Features/Dashboard/Queries/GetDashboardSummary/*` | Baru | Query, Validator (7/30 hari), Handler agregasi SQL |
| 7 | `src/HelpDeskPro.Application/Features/Dashboard/Queries/GetAgentWorkload/*` | Baru | Query dan Handler beban kerja agent |
| 8 | `src/HelpDeskPro.Application/Features/Dashboard/Queries/GetPendingEscalations/*` | Baru | Query dan Handler daftar eskalasi belum di-acknowledge |
| 9 | `src/HelpDeskPro.Application/Features/Master/Departments/Models/DepartmentDto.cs` | Baru | DTO departemen |
| 10 | `src/HelpDeskPro.Application/Features/Master/Departments/Queries/*` | Baru | Query `GetDepartments` & `GetDepartmentById` |
| 11 | `src/HelpDeskPro.Application/Features/Master/Departments/Commands/*` | Baru | Command `Create`, `Update`, `ToggleStatus`, `Delete` (dependency check) |
| 12 | `src/HelpDeskPro.Application/Features/Master/Categories/Models/CategoryDto.cs` | Baru | DTO kategori |
| 13 | `src/HelpDeskPro.Application/Features/Master/Categories/Queries/*` | Baru | Query `GetCategories` & `GetCategoryById` |
| 14 | `src/HelpDeskPro.Application/Features/Master/Categories/Commands/*` | Baru | Command `Create`, `Update`, `ToggleStatus`, `Delete` (dependency check) |
| 15 | `src/HelpDeskPro.Application/Features/Master/Users/Models/*` | Baru | DTO `UserListItemDto` & `UserDetailDto` |
| 16 | `src/HelpDeskPro.Application/Features/Master/Users/Queries/*` | Baru | Query `GetUsers` (paging, filter, search) & `GetUserById` |
| 17 | `src/HelpDeskPro.Application/Features/Master/Users/Commands/*` | Baru | Command `Create`, `Update`, `ResetPassword`, `ToggleStatus`, `Delete` |
| 18 | `src/HelpDeskPro.Application/Features/Master/SlaPolicies/Models/SlaPolicyDto.cs` | Baru | DTO kebijakan SLA |
| 19 | `src/HelpDeskPro.Application/Features/Master/SlaPolicies/Queries/GetSlaPolicies/*` | Baru | Query daftar kebijakan SLA |
| 20 | `src/HelpDeskPro.Application/Features/Master/SlaPolicies/Commands/UpdateSlaPolicy/*` | Baru | Command, Validator, Handler ubah nilai SLA (FR-34) |
| 21 | `src/HelpDeskPro.Api/Controllers/DashboardController.cs` | Baru | Controller dashboard supervisor & admin |
| 22 | `src/HelpDeskPro.Api/Controllers/MasterDepartmentsController.cs` | Baru | Controller master departemen |
| 23 | `src/HelpDeskPro.Api/Controllers/MasterCategoriesController.cs` | Baru | Controller master kategori |
| 24 | `src/HelpDeskPro.Api/Controllers/MasterUsersController.cs` | Baru | Controller master user |
| 25 | `src/HelpDeskPro.Api/Controllers/MasterSlaPoliciesController.cs` | Baru | Controller master kebijakan SLA |
| 26 | `tests/HelpDeskPro.UnitTests/Features/Dashboard/*` | Baru | Unit test query summary, workload, pending escalations |
| 27 | `tests/HelpDeskPro.UnitTests/Features/Master/*` | Baru | Unit test departemen, kategori, user, SLA policy |
| 28 | `tests/HelpDeskPro.IntegrationTests/Controllers/DashboardControllerIntegrationTests.cs` | Baru | Integration test dashboard scoping & role access |
| 29 | `tests/HelpDeskPro.IntegrationTests/Controllers/MasterControllersIntegrationTests.cs` | Baru | Integration test CRUD master data & role policy |

