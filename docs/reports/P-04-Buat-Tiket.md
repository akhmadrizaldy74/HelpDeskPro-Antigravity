# Laporan Implementasi — P-04 Fitur Buat Tiket

| Item | Isi |
|---|---|
| Modul | Buat Tiket (FR-03 s.d. FR-06, AC-01, AC-02) |
| Tanggal | 9 November 2026 |
| Target Framework | .NET 8 |
| Status | Selesai & Lulus Pengujian (57/57 Tests Passed) |

---

## 1. Ringkasan Implementasi

Pada tahap **P-04 · Fitur Buat Tiket**, kami telah mengimplementasikan seluruh kebutuhan fungsional dan kriteria penerimaan pembuatan tiket:
1. **Domain Method `Ticket.Create(...)`**:
   - Menghitung `ResponseDueAt` dan `ResolutionDueAt` otomatis berdasarkan `SlaPolicy` dan `IDateTimeProvider.UtcNow`.
   - Mengisi status awal `TicketStatus.Open`.
   - Mencatat riwayat awal otomatis pada `TicketHistories` dengan aksi "Created".
2. **Interface `ITicketNumberGenerator` & Implementasi `TicketNumberGenerator`**:
   - Format: `HD-{YYYY}-{000000}` (contoh: `HD-2026-000001`).
   - Aman dari race condition konkurensi database dengan transaksi SQL Server dan locking atomic `WITH (UPDLOCK, ROWLOCK)`.
   - Reset penomoran sequence per tahun secara otomatis.
   - Fallback thread-safe dengan `SemaphoreSlim` untuk pengujian non-relasional/in-memory.
3. **Application Layer (CQRS & MediatR)**:
   - `CreateTicketCommand` (Title, Description, CategoryId, Priority).
   - `CreateTicketCommandValidator` (FluentValidation: Judul 5–200 karakter, CategoryId > 0, Priority enum valid).
   - `CreateTicketCommandHandler` (validasi Category, ambil SLA, generate sequence nomor, panggil `Ticket.Create`, simpan ke DB, dan kembalikan `TicketDetailDto`).
4. **API Controller**:
   - `POST /api/tickets` dengan `[Authorize]`, mengembalikan HTTP 201 Created dengan header `Location` (`/api/Tickets/{id}`) dan payload `ApiResponse<TicketDetailDto>`.
   - `GET /api/tickets/{id}` sebagai target endpoint route.
5. **Unit & Integration Tests**:
   - **51 Unit Tests** mencakup validator bounds (empty, <5, >200 chars, invalid category), kalkulasi SLA due date untuk 4 prioritas (Critical, High, Medium, Low), format tiket, rollover pergantian tahun, command handlers, dan pipeline behaviors.
   - **6 Integration Tests** termasuk uji beban 20 request paralel serentak (AC-02) yang menghasilkan 20 nomor tiket unik berurutan tanpa tabrakan nomor, serta verifikasi kalkulasi SLA (AC-01).
   - Code coverage layer `HelpDeskPro.Application`: **88.53%** (melebihi target ≥ 70%).

---

## 2. Struktur File yang Dibuat & Diubah

| Layer | File | Keterangan |
|---|---|---|
| Domain | `Entities/Ticket.cs` | Penambahan domain factory method `Ticket.Create(...)` + audit history |
| Application | `Common/Interfaces/ITicketNumberGenerator.cs` | Interface generator sequence nomor tiket |
| Application | `Features/Tickets/Models/TicketDetailDto.cs` | DTO detail tiket respons endpoint |
| Application | `Features/Tickets/Commands/CreateTicket/CreateTicketCommand.cs` | MediatR command buat tiket |
| Application | `Features/Tickets/Commands/CreateTicket/CreateTicketCommandValidator.cs` | FluentValidation rules sesuai FR-03 |
| Application | `Features/Tickets/Commands/CreateTicket/CreateTicketCommandHandler.cs` | CQRS handler orchestrator |
| Infrastructure | `Services/TicketNumberGenerator.cs` | Implementasi atomic sequence lock SQL Server `UPDLOCK, ROWLOCK` |
| Infrastructure | `DependencyInjection.cs` | Registrasi `ITicketNumberGenerator` |
| Api | `Controllers/TicketsController.cs` | Controller endpoint `POST /api/tickets` & `GET /api/tickets/{id}` |
| UnitTests | `Features/Tickets/CreateTicketCommandValidatorTests.cs` | Pengujian batas validasi judul & kategori |
| UnitTests | `Features/Tickets/TicketDomainTests.cs` | Pengujian SLA 4 level prioritas & status awal |
| UnitTests | `Features/Tickets/TicketNumberGeneratorTests.cs` | Pengujian format & reset nomor tahun baru |
| UnitTests | `Features/Tickets/CreateTicketCommandHandlerTests.cs` | Pengujian alur handler, not found, unauth, sukses |
| IntegrationTests | `Controllers/TicketsControllerIntegrationTests.cs` | Uji konkurensi 20 request paralel (AC-02) & SLA (AC-01) |

---

## 3. Hasil Pengujian

```
Passed! - Failed: 0, Passed: 51, Skipped: 0, Total: 51 (HelpDeskPro.UnitTests.dll)
Passed! - Failed: 0, Passed:  6, Skipped: 0, Total:  6 (HelpDeskPro.IntegrationTests.dll)
Total: 57 Passed, 0 Warnings, 0 Errors
Application Code Coverage: 88.53%
```

