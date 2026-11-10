# Laporan Implementasi — P-06 Assign & Alur Status

| Item | Isi |
|---|---|
| Modul | Assign & Alur Status Tiket (FR-10 s.d. FR-15, FR-27, AC-03, AC-07) |
| Tanggal | 9 November 2026 |
| Target Framework | .NET 8 |
| Status | Selesai & Lulus Pengujian (172/172 Tests Passed) |

---

## 1. Ringkasan Implementasi

Pada tahap **P-06 · Assign & Alur Status**, kami telah mengimplementasikan logika penugasan (assign) dan transisi alur status tiket secara menyeluruh sesuai FSD 4.4, 4.5, AC-03, dan AC-07:

1. **Domain Logic Alur Transisi Status (`Ticket.ChangeStatus`)**:
   - Diterapkan sebagai domain method murni pada entitas `Ticket` (bukan di MediatR handler).
   - Menggunakan tabel matriks transisi status yang eksplisit:
     - `Open` $\rightarrow$ `InProgress`, `Cancelled`
     - `InProgress` $\rightarrow$ `Pending`, `Resolved`
     - `Pending` $\rightarrow$ `InProgress`
     - `Resolved` $\rightarrow$ `Closed`, `InProgress` (maksimal 7 hari)
     - `Closed` dan `Cancelled` adalah status terminal.
   - Transisi di luar diagram ditolak melempar `BusinessRuleException` dengan pesan persis sesuai **AC-03**:
     `"Perubahan status dari {OldStatus} ke {NewStatus} tidak diizinkan."`
   - Batasan Role:
     - **Requester**: hanya boleh membatalkan tiket (`Open` $\rightarrow$ `Cancelled`) dan menutup tiket (`Resolved` $\rightarrow$ `Closed`).
     - **Agent / Supervisor / Admin**: dapat menjalankan transisi operasional.
   - Aturan Khusus Status:
     - Pindah ke `Pending` wajib menyertakan alasan (disimpan sebagai komentar internal `TicketComment` dengan `IsInternal = true`) dan mengisi `PausedAt = utcNow`.
     - Pindah ke `Resolved` wajib menyertakan catatan penyelesaian dan mengisi `ResolvedAt = utcNow`.
     - Reopen tiket (`Resolved` $\rightarrow$ `InProgress`) dibatasi maksimal 7 hari sejak `ResolvedAt`. Lebih dari 7 hari ditolak.
     - Pindah ke `Closed` mengisi `ClosedAt = utcNow`.
   - Pencatatan riwayat perubahan: setiap transisi status menambahkan entri `TicketHistory` (`FieldName = "Status"`).

2. **Pause & Resume SLA Resolusi (FR-27, AC-07)**:
   - Saat status berpindah ke `Pending`, sistem mencatat timestamp `PausedAt = utcNow`.
   - Saat status keluar dari `Pending` (kembali ke `InProgress`):
     - Menghitung durasi jeda dalam menit: `var pausedMinutes = (int)Math.Round((utcNow - PausedAt.Value).TotalMinutes)`.
     - Mengakumulasikan ke `TotalPausedMinutes += pausedMinutes`.
     - Menggeser tenggat waktu resolusi persis sebesar jeda: `ResolutionDueAt = ResolutionDueAt.AddMinutes(pausedMinutes)`.
     - Mengosongkan `PausedAt = null`.
   - Teruji sesuai **AC-07**: tiket yang berstatus Pending selama 2 jam lalu kembali ke InProgress mengalami pergeseran `ResolutionDueAt` persis +2 jam (120 menit).

3. **Penugasan Tiket (`Ticket.Assign` & `AssignTicketCommand`)**:
   - Aturan hak penugasan (FR-10):
     - **Agent**: hanya boleh mengambil tiket untuk dirinya sendiri (self-assign) dan hanya jika tiket belum memiliki penangan (`AssigneeId == null`).
     - **Supervisor & Admin**: dapat menugaskan tiket kepada staf mana pun di departemen IT.
     - **Requester**: tidak memiliki wewenang untuk menugaskan tiket.
     - Target assignee wajib berada di departemen IT (`Department.Code == "IT"`).
   - Efek assign pertama kali (FR-11):
     - Mengisi `FirstRespondedAt = utcNow` jika belum terisi.
     - Jika status masih `Open`, otomatis mengubah status menjadi `InProgress`.
     - Mencatat `TicketHistory` untuk `Assignee` dan (jika terjadi) `Status`.

4. **Optimistic Concurrency Control (`RowVersion`)**:
   - `AssignTicketCommand` dan `ChangeTicketStatusCommand` mewajibkan `RowVersion` (byte[]).
   - Deteksi konflik konkurensi: jika terjadi ketidaksesuaian `RowVersion` atau terjadi `DbUpdateConcurrencyException`, `GlobalExceptionMiddleware` menangkapnya dan mengembalikan status **HTTP 409 Conflict** dengan pesan:
     `"Tiket sudah diubah pengguna lain, muat ulang halaman."`

5. **API Endpoints (`TicketsController`)**:
   - `PATCH /api/tickets/{id}/assign`: menerima body `{ assigneeId, rowVersion }`, mengembalikan `200 OK` dengan `ApiResponse<TicketDetailDto>`.
   - `PATCH /api/tickets/{id}/status`: menerima body `{ status, reason, rowVersion }`, mengembalikan `200 OK` dengan `ApiResponse<TicketDetailDto>`.

6. **Pengujian Menyeluruh (172/172 Tests Passed)**:
   - **159 Unit Tests**:
     - `TicketStatusTransitionTests`: seluruh kombinasi transisi valid (7 alur) dan tidak valid (29 pasang status) diuji dalam `[Theory]`, validasi pesan AC-03, aturan batas reopen 7 hari, validasi alasan/catatan Pending & Resolved, pause/resume SLA 2 jam (AC-07), dan hak akses Requester.
     - `TicketAssignTests`: self-assign Agent, larangan assign tiket orang lain / tiket yang sudah bertuan, hak Supervisor & Admin, proteksi role Requester, penanganan `FirstRespondedAt`, dan riwayat history.
     - `AssignTicketCommandHandlerTests` & `ChangeTicketStatusCommandHandlerTests`: skenario sukses, not found, unauthenticated, non-IT assignee, dan konflik `RowVersion` 409.
     - Validator tests: FluentValidation untuk ID, status enum, alasan wajib, dan `RowVersion`.
   - **13 Integration Tests**:
     - `PATCH /api/tickets/{id}/assign` berhasil dan mengubah status `Open` $\rightarrow$ `InProgress`.
     - `PATCH /api/tickets/{id}/status` dari `Open` langsung ke `Resolved` mengembalikan **422 UnprocessableEntity** (AC-03).
     - `PATCH /api/tickets/{id}/status` dengan `RowVersion` usang mengembalikan **409 Conflict**.
   - Code Coverage:
     - `HelpDeskPro.Application`: **91.36%** (target $\ge 70\%$).
     - `HelpDeskPro.Domain`: **79.91%** (target $\ge 70\%$).

---

## 2. Struktur File yang Dibuat & Diubah

| Layer | File | Keterangan |
|---|---|---|
| Domain | `Entities/Ticket.cs` | Matriks `_allowedTransitions`, method `ChangeStatus(...)`, dan method `Assign(...)` |
| Application | `Features/Tickets/Commands/AssignTicket/AssignTicketCommand.cs` | MediatR command assign tiket |
| Application | `Features/Tickets/Commands/AssignTicket/AssignTicketCommandValidator.cs` | FluentValidation command assign tiket |
| Application | `Features/Tickets/Commands/AssignTicket/AssignTicketCommandHandler.cs` | Handler assign tiket dengan validasi IT dept & konkurensi |
| Application | `Features/Tickets/Commands/ChangeTicketStatus/ChangeTicketStatusCommand.cs` | MediatR command ubah status tiket |
| Application | `Features/Tickets/Commands/ChangeTicketStatus/ChangeTicketStatusCommandValidator.cs` | FluentValidation alur status tiket |
| Application | `Features/Tickets/Commands/ChangeTicketStatus/ChangeTicketStatusCommandHandler.cs` | Handler alur status tiket dengan validasi role & konkurensi |
| Application | `Features/Tickets/Models/TicketDetailDto.cs` | Penambahan properti `RowVersion` |
| Api | `Middlewares/GlobalExceptionMiddleware.cs` | Penanganan `DbUpdateConcurrencyException` $\rightarrow$ 409 Conflict |
| Api | `Controllers/TicketsController.cs` | Endpoint `PATCH /api/tickets/{id}/assign` dan `PATCH /api/tickets/{id}/status` |
| Tests | `UnitTests/Features/Tickets/TicketStatusTransitionTests.cs` | Unit tests seluruh kombinasi transisi, reopen, SLA pause/resume, role |
| Tests | `UnitTests/Features/Tickets/TicketAssignTests.cs` | Unit tests aturan assignment, FirstRespondedAt, IT dept |
| Tests | `UnitTests/Features/Tickets/AssignTicketCommandHandlerTests.cs` | Unit tests handler assignment |
| Tests | `UnitTests/Features/Tickets/AssignTicketCommandValidatorTests.cs` | Unit tests validator command assign |
| Tests | `UnitTests/Features/Tickets/ChangeTicketStatusCommandHandlerTests.cs` | Unit tests handler perubahan status |
| Tests | `UnitTests/Features/Tickets/ChangeTicketStatusCommandValidatorTests.cs` | Unit tests validator perubahan status |
| Tests | `IntegrationTests/Controllers/TicketsControllerIntegrationTests.cs` | Integration tests assign, illegal transition 422 (AC-03), RowVersion 409 |

