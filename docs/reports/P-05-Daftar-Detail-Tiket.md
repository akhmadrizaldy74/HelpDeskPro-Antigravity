# Laporan Implementasi — P-05 Daftar & Detail Tiket

| Item | Isi |
|---|---|
| Modul | Daftar & Detail Tiket (FR-07 s.d. FR-09, AC-05) |
| Tanggal | 9 November 2026 |
| Target Framework | .NET 8 |
| Status | Selesai & Lulus Pengujian (86/86 Tests Passed) |

---

## 1. Ringkasan Implementasi

Pada tahap **P-05 · Daftar & Detail Tiket**, kami telah mengimplementasikan query daftar dan detail tiket sesuai seluruh spesifikasi FSD 3.1 dan 4.3:

1. **`GetTicketsQuery` & Handler**:
   - Filter lengkap: `statuses` (multi), `priorities` (multi), `categoryId`, `assigneeId`, `unassigned` (bool), `dateFrom`, `dateTo`, `search` (TicketNo / judul), `page` (default 1), `pageSize` (default 20, maks 100).
   - Hak lihat berbasis role diterapkan langsung di query (bukan di controller):
     - **Requester**: hanya tiket milik sendiri (`RequesterId == currentUserId`).
     - **Agent**: tiket yang di-assign ke dirinya ATAU belum di-assign (`AssigneeId == currentUserId || AssigneeId == null`).
     - **Supervisor**: seluruh tiket di departemennya (IT Supervisor mengawasi seluruh tiket IT).
     - **Admin**: melihat seluruh tiket tanpa batasan.
   - Urutan default sesuai **FR-08**: tiket SLA lewat paling atas, lalu `ResolutionDueAt` terdekat (ascending).
   - Optimasi kueri: memakai `AsNoTracking()` dan proyeksi LINQ `.Select(...)`, tidak memuat relasi entity penuh ke change tracker.
   - Hasil dibungkus dalam `PagedResult<TicketListItemDto>`.

2. **`GetTicketByIdQuery` & Handler**:
   - Mengambil detail tiket lengkap berdasarkan `Id`.
   - Hak akses konsisten dengan daftar: tiket di luar hak akses melempar `NotFoundException` (menghasilkan status HTTP 404 agar keberadaan tiket tidak bocor).
   - `Timeline`: gabungan komentar (`TicketComment`) dan riwayat perubahan (`TicketHistory`), diurutkan kronologis berdasarkan waktu (`Timestamp` ascending).
   - **Komentar internal**: disembunyikan secara otomatis ketika pengakses adalah user dengan role `Requester` (**AC-05**).
   - Menghitung `SlaRemainingMinutes` (negatif jika lewat) melalui `SlaCalculator`, memperhitungkan status `Pending`, `PausedAt`, dan `TotalPausedMinutes`.

3. **API Endpoints (`TicketsController`)**:
   - `GET /api/tickets`: endpoint daftar dengan query string filter & paging, mengembalikan `ApiResponse<PagedResult<TicketListItemDto>>`.
   - `GET /api/tickets/{id}`: endpoint detail tiket, mengembalikan `ApiResponse<TicketDetailDto>` atau 404 jika tidak ditemukan/tidak berhak.

4. **Pengujian (86/86 Tests Passed)**:
   - **76 Unit Tests**: filter, paging, hak lihat 4 role, kalkulasi `SlaRemainingMinutes` (normal, overdue, resolved, pending), dan timeline gabungan dengan masking komentar internal (AC-05).
   - **10 Integration Tests**: HTTP request end-to-end untuk pembuatan tiket paralel (AC-02), kalkulasi SLA (AC-01), scoping daftar tiket berbasis role, 404 akses tiket orang lain, dan masking komentar internal (AC-05).
   - Code coverage layer `HelpDeskPro.Application`: **86.54%** (melebihi target $\ge 70\%$).

---

## 2. Struktur File yang Dibuat & Diubah

| Layer | File | Keterangan |
|---|---|---|
| Application | `Features/Tickets/Models/TicketListItemDto.cs` | DTO item daftar tiket |
| Application | `Features/Tickets/Models/TimelineItemDto.cs` | DTO timeline gabungan komentar + history |
| Application | `Features/Tickets/Models/TicketAttachmentDto.cs` | DTO lampiran tiket |
| Application | `Features/Tickets/Models/TicketDetailDto.cs` | Update properti Timeline, Attachments, SlaRemainingMinutes |
| Application | `Features/Tickets/Common/SlaCalculator.cs` | Helper kalkulasi sisa menit SLA |
| Application | `Features/Tickets/Queries/GetTickets/GetTicketsQuery.cs` | MediatR query daftar tiket |
| Application | `Features/Tickets/Queries/GetTickets/GetTicketsQueryValidator.cs` | Validasi page, pageSize, rentang tanggal |
| Application | `Features/Tickets/Queries/GetTickets/GetTicketsQueryHandler.cs` | Handler daftar tiket dengan role scoping, filter, order, dan proyeksi |
| Application | `Features/Tickets/Queries/GetTicketById/GetTicketByIdQuery.cs` | MediatR query detail tiket |
| Application | `Features/Tickets/Queries/GetTicketById/GetTicketByIdQueryHandler.cs` | Handler detail tiket dengan timeline & masking AC-05 |
| Application | `Common/Interfaces/ICurrentUserService.cs` | Penambahan properti `Role` |
| Api | `Services/CurrentUserService.cs` | Pembacaan claim role dari `HttpContext` |
| Api | `Controllers/TicketsController.cs` | Endpoint `GET /api/tickets` dan `GET /api/tickets/{id}` |
| UnitTests | `Features/Tickets/GetTicketsQueryValidatorTests.cs` | Unit test validasi input query daftar |
| UnitTests | `Features/Tickets/SlaCalculatorTests.cs` | Unit test kalkulasi SLA remaining |
| UnitTests | `Features/Tickets/GetTicketsQueryHandlerTests.cs` | Unit test filter, paging, hak lihat 4 role, sorting |
| UnitTests | `Features/Tickets/GetTicketByIdQueryHandlerTests.cs` | Unit test detail tiket, 404 not found, masking komentar AC-05 |
| IntegrationTests | `Controllers/TicketsControllerIntegrationTests.cs` | Integration test endpoint daftar, detail, role access, AC-05 |

---

## 3. Hasil Pengujian

```
Passed! - Failed: 0, Passed: 76, Skipped: 0, Total: 76 (HelpDeskPro.UnitTests.dll)
Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10 (HelpDeskPro.IntegrationTests.dll)
Total: 86 Passed, 0 Warnings, 0 Errors
Application Line Coverage: 86.54%
```

