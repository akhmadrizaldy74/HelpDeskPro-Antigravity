# Coding Standard — HelpDeskPro

Rules ini berlaku untuk SEMUA pekerjaan di workspace ini. Baca sebelum membuat plan atau menulis kode.

## 1. Cara Kerja Agent
- Selalu buat **Implementation Plan** dan **Task List** terlebih dahulu. JANGAN menulis/mengubah kode sebelum plan disetujui user.
- Kerjakan hanya scope yang diminta di prompt. Jangan menambah fitur, halaman, atau package di luar scope.
- Jika FSD ambigu atau bertentangan, BERHENTI dan tanyakan. Jangan berasumsi diam-diam.
- Setelah selesai: jalankan `dotnet build` dan `dotnet test`, lalu buat **Walkthrough** berisi: file yang diubah, keputusan desain, cara menguji, dan risiko/hal yang belum selesai.
- Jangan pernah menghapus, men-skip, atau menonaktifkan test agar build hijau.

## 2. Arsitektur (Clean Architecture)
- `HelpDeskPro.Domain` → tidak boleh mereferensi project lain. Berisi Entity, Enum, Value Object, Domain Exception, domain rule.
- `HelpDeskPro.Application` → referensi Domain saja. Berisi Command/Query (MediatR), Handler, DTO, Validator (FluentValidation), Interface (repository, service, `IDateTimeProvider`).
- `HelpDeskPro.Infrastructure` → implementasi Interface: EF Core DbContext, Configuration, Migration, Repository, Email, Background Job.
- `HelpDeskPro.Api` → Controller tipis: terima request → kirim ke MediatR → kembalikan response. Tidak ada business logic di controller.
- `HelpDeskPro.Web` → Blazor UI, memanggil Api melalui typed HttpClient.
- Struktur fitur: `Application/Features/{Fitur}/Commands/{NamaCommand}/` berisi `XxxCommand.cs`, `XxxCommandHandler.cs`, `XxxCommandValidator.cs`. Query mengikuti pola yang sama di `Queries/`.

## 3. Penamaan
- Class, method, property, public member: `PascalCase`.
- Private field: `_camelCase`. Parameter & variabel lokal: `camelCase`.
- Interface diawali `I` (`ITicketRepository`).
- Method async wajib suffix `Async` dan menerima `CancellationToken`.
- DTO: `XxxDto`, request: `XxxRequest`, response: `XxxResponse`.
- Nama tabel jamak (`Tickets`), nama entity tunggal (`Ticket`).
- Identifier kode dalam bahasa Inggris. Pesan untuk user (validasi, error, UI) dalam bahasa Indonesia.

## 4. Aturan C#
- `Nullable` enable, `TreatWarningsAsErrors` true, `ImplicitUsings` enable (atur di `Directory.Build.props`).
- Gunakan file-scoped namespace dan primary constructor bila sesuai.
- Tidak ada magic string/number → gunakan `const`, `enum`, atau Options pattern.
- Waktu selalu lewat `IDateTimeProvider`. Simpan UTC di database, konversi ke WIB (Asia/Jakarta) hanya di UI.
- Jangan pakai `.Result` / `.Wait()`. Semua I/O async.
- Satu class per file. Maksimal ±300 baris per file; pecah jika lebih.
- Versi package diatur terpusat di `Directory.Packages.props`.

## 5. Data & EF Core
- Konfigurasi entity pakai `IEntityTypeConfiguration<T>` di `Persistence/Configurations`, bukan Data Annotation.
- Entity utama punya kolom audit `CreatedAt, CreatedBy, UpdatedAt, UpdatedBy`, diisi otomatis lewat `SaveChangesInterceptor`.
- Soft delete via `IsDeleted` + global query filter.
- `Ticket` memakai `RowVersion` untuk optimistic concurrency.
- JANGAN mengubah migration yang sudah ada. Selalu buat migration baru dengan nama deskriptif.
- Query read-only pakai `AsNoTracking()`. Semua daftar data wajib paging (default 20, maks 100).

## 6. API
- Format response seragam: `ApiResponse<T> { bool Success, T? Data, IReadOnlyList<string> Errors, string TraceId }`.
- Error ditangani global exception middleware → `ProblemDetails` untuk 4xx/5xx.
- Validasi lewat FluentValidation di pipeline behavior MediatR, bukan di controller.
- Setiap endpoint diberi `[Authorize]` sesuai matriks role di FSD dan terdokumentasi di Swagger/OpenAPI.

## 7. Logging & Security
- Logging pakai Serilog, structured: `logger.LogInformation("Ticket {TicketNo} created", ticketNo)`. Bukan string interpolasi.
- Jangan log password, token, atau isi attachment.
- Secret tidak boleh di `appsettings.json` yang di-commit → gunakan User Secrets / environment variable.

## 8. Testing
- Setiap Command/Query handler dan Validator baru WAJIB punya unit test.
- Framework: xUnit + FluentAssertions + NSubstitute.
- Penamaan test: `MethodName_Scenario_ExpectedResult`.
- Pola AAA (Arrange, Act, Assert) dengan komentar pemisah.
- Integration test pakai `WebApplicationFactory` + Testcontainers SQL Server.
- Target coverage layer Application ≥ 70%.

## 9. Git
- Format commit: `type(scope): deskripsi` → `feat(ticket): add create ticket command`.
- Type: feat, fix, refactor, test, docs, chore.
