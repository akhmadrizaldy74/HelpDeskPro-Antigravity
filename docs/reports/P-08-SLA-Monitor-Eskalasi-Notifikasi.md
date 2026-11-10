# Laporan Implementasi — P-08 SLA Monitor, Eskalasi, Notifikasi

| Item | Isi |
|---|---|
| Modul | SLA Monitor, Eskalasi, Notifikasi, & Auto-Close (FR-15, FR-20 s.d. FR-29, AC-04) |
| Tanggal | 10 November 2026 |
| Target Framework | .NET 8 |
| Status | Selesai & Lulus Pengujian (266/266 Tests Passed) |

---

## 1. Ringkasan Implementasi

Pada tahap **P-08 · SLA Monitor, Eskalasi, Notifikasi**, kami telah mengimplementasikan layanan pemantau SLA berkala (*background worker*), deteksi dan eskalasi pelanggaran SLA (Response & Resolution Breach), eskalasi manual, konfirmasi eskalasi (*acknowledge*), penutupan otomatis (*auto-close* tiket terselesaikan $\ge 3$ hari), serta sistem notifikasi in-app untuk seluruh *lifecycle* tiket sesuai spesifikasi FSD 4.8, 4.9 (FR-20 s.d. FR-29), AC-04, dan FR-15:

1. **Layanan SLA Monitor (`SlaMonitorService` & `CheckSlaBreachesCommand`)**:
   - **`SlaMonitorService`**: Terdaftar sebagai `IHostedService` (`BackgroundService`) di Infrastructure. Membaca konfigurasi `SlaMonitor:IntervalMinutes` (default 5 menit). Pada setiap iterasi, service membuat scoped `ISender` untuk memanggil `CheckSlaBreachesCommand` sehingga seluruh logika inti SLA terisolasi di Application layer dan dapat diuji secara deterministik tanpa dependensi timer background.
   - **Perilaku Idempoten (FR-24)**: Eksekusi `CheckSlaBreachesCommand` berulang kali tidak pernah menghasilkan duplikasi eskalasi untuk pelanggaran yang sama.

2. **Deteksi Pelanggaran SLA & Aturan Eskalasi (FR-22, FR-23, AC-04, FR-27)**:
   - **SLA Response Breach (FR-22, AC-04)**:
     - Kondisi: `Ticket.FirstRespondedAt == null` dan `CurrentTime > Ticket.ResponseDueAt` pada tiket berstatus aktif.
     - Aksi: Mencatat `TicketEscalation` Level 1 dengan alasan `EscalationReason.SlaResponseBreach`.
     - Penerima Eskalasi: `SupervisorId` dari `Assignee`. Jika tiket belum di-assign, penerima diarahkan ke Supervisor departemen IT. Jika supervisor tidak ditemukan, sistem mencatat log peringatan (*warning*) dan melanjutkan pemrosesan tanpa gagal.
     - Mengirim notifikasi eskalasi kepada supervisor terkait (**FR-28**).
   - **SLA Resolution Breach (FR-23)**:
     - Kondisi: `Ticket.Status` bukan `Resolved`/`Closed`/`Cancelled`/`Pending` dan `CurrentTime > Ticket.ResolutionDueAt`.
     - Aksi: Menandai `Ticket.IsSlaBreached = true`, mencatat `TicketEscalation` Level 2 dengan alasan `EscalationReason.SlaResolutionBreach`.
     - Penerima Eskalasi: Supervisor Level 2 (yaitu supervisor dari supervisor Level 1). Jika tidak ditemukan, fallback ke supervisor departemen IT.
   - **Pause SLA Saat Status Pending (FR-27)**:
     - Tiket dengan status `Pending` diabaikan dari pemeriksaan SLA Resolution Breach sehingga waktu penundaan tidak memicu eskalasi keliru.

3. **Penutupan Otomatis Tiket / Auto-Close (FR-15)**:
   - Kondisi: Tiket berstatus `Resolved` selama $\ge 3$ hari (72 jam) sejak `ResolvedAt` tanpa ada sanggahan/tanggapan lanjutan dari Requester.
   - Aksi: Status tiket secara otomatis diperbarui dari `Resolved` menjadi `Closed`, mencatat `TicketHistory` (*"Closed"*, *"Ditutup otomatis oleh sistem setelah 3 hari diselesaikan tanpa tanggapan"*), dan mengirimkan notifikasi penutupan kepada Requester (**FR-28**).

4. **Eskalasi Manual & Konfirmasi Eskalasi (FR-25, FR-26)**:
   - **`EscalateTicketCommand` (FR-25)**:
     - Memungkinkan Agent, Supervisor, atau Admin mengeskalasi tiket secara manual (`EscalationReason.Manual`).
     - Validasi: Catatan eskalasi (`ReasonNote`) wajib diisi minimal 10 karakter dan maksimal 1000 karakter.
     - Penentuan target: otomatis ke supervisor yang berwenang.
   - **`AcknowledgeEscalationCommand` (FR-26)**:
     - Memungkinkan penerima eskalasi (atau Admin/Supervisor terkait) melakukan konfirmasi bahwa tiket eskalasi telah diperhatikan.
     - Mengisi `AcknowledgedAt` dengan waktu saat ini (`utcNow`).

5. **Layanan Notifikasi In-App (`INotificationService` & Endpoints FR-28, FR-29)**:
   - **`INotificationService`**:
     - Mengirim notifikasi otomatis untuk 4 tipe kejadian penting:
       1. `TicketAssigned`: Saat tiket di-assign ke Agent (**FR-10**).
       2. `NewComment`: Saat ada komentar baru (notifikasi ke Requester atau Assignee) (**FR-16**).
       3. `StatusChanged`: Saat status tiket berubah (**FR-11** s.d. **FR-15**).
       4. `Escalation`: Saat eskalasi otomatis maupun manual dibuat (**FR-22**, **FR-23**, **FR-25**).
   - **Query & Mutasi Notifikasi (FR-29)**:
     - `GET /api/notifications`: Mengambil daftar notifikasi milik pengguna saat ini (termasuk status `IsRead`).
     - `GET /api/notifications/unread-count`: Menghitung jumlah notifikasi yang belum dibaca (`unreadCount`).
     - `PATCH /api/notifications/read-all`: Menandai seluruh notifikasi yang belum dibaca milik pengguna saat ini menjadi terbaca (`IsRead = true`).

6. **API Endpoints Baru & Terintegrasi**:
   - `POST /api/tickets/{id}/escalate`: Eskalasi manual tiket (200 OK).
   - `PATCH /api/escalations/{id}/acknowledge`: Konfirmasi tiket eskalasi (200 OK).
   - `GET /api/notifications`: Daftar notifikasi user (200 OK).
   - `GET /api/notifications/unread-count`: Jumlah notifikasi belum dibaca (200 OK).
   - `PATCH /api/notifications/read-all`: Tandai semua notifikasi terbaca (200 OK).

---

## 2. Hasil Pengujian & Verifikasi Kualitas

### A. Rekapitulasi Test Execution
- **Unit Tests**: **241 / 241 passed** (100% lulus, 0 failed, 0 skipped).
- **Integration Tests**: **25 / 25 passed** (100% lulus, 0 failed, 0 skipped).
- **Total Tests**: **266 / 266 passed**.

### B. Cakupan Kode (Code Coverage)
Berdasarkan laporan *Coverlet XPlat Code Coverage*:
- **`HelpDeskPro.Application`**: **88.97%** line coverage (Standar FSD: $\ge 70\%$).
- **`HelpDeskPro.Domain`**: **90.35%** line coverage (Standar FSD: $\ge 70\%$).
- **Kepatuhan Warning**: **0 Warnings, 0 Errors** pada kompilasi Release (`TreatWarningsAsErrors=true`).

### C. Ringkasan Kasus Uji Kunci SLA & Eskalasi
1. `Handle_TicketBeforeResponseDueDate_DoesNotEscalate`: Memastikan tiket sebelum batas waktu response tidak memicu eskalasi.
2. `Handle_TicketPastResponseDueDate_WithoutFirstResponse_EscalatesToLevel1`: Memastikan pelanggaran response SLA memicu eskalasi Level 1 dan mengirimkan notifikasi.
3. `Handle_TicketPastResponseDueDate_WithFirstResponse_DoesNotEscalateResponse`: Memastikan tiket yang sudah direspon tidak dieskalasi atas nama SLA response.
4. `Handle_TicketPastResolutionDueDate_MarksBreachedAndEscalatesLevel2`: Memastikan pelanggaran resolusi menandai `IsSlaBreached = true` dan mengeskalasi Level 2.
5. `Handle_WhenRunTwice_DoesNotCreateDuplicateEscalations`: Memastikan sifat idempoten (**FR-24**), menjalankan pemantauan 2x tidak memicu eskalasi ganda.
6. `Handle_TicketInPendingStatus_DoesNotBreachResolutionSla`: Memastikan tiket berstatus `Pending` di-pause dan tidak dieskalasi resolusinya (**FR-27**).
7. `Handle_ResolvedTicketPast3Days_AutoClosesTicket`: Memastikan auto-close setelah 3 hari (**FR-15**).
8. `Handle_WhenNoSupervisorFound_LogsWarningAndContinuesWithoutException`: Memastikan penanganan graceful fallback jika supervisor tidak ditemukan.
9. `AcknowledgeEscalation`: Memverifikasi timestamp `AcknowledgedAt` terekam dengan akurat.
10. `ManualEscalation`: Memverifikasi validasi catatan eskalasi ($\ge 10$ karakter) dan pencatatan eskalasi manual.

---

## 3. Daftar Berkas Baru dan Dimodifikasi

| No | Berkas | Status | Keterangan |
|---|---|---|---|
| 1 | `src/HelpDeskPro.Application/Common/Interfaces/INotificationService.cs` | Baru | Kontrak interface notifikasi |
| 2 | `src/HelpDeskPro.Application/Features/Notifications/Models/NotificationDto.cs` | Baru | DTO representasi notifikasi |
| 3 | `src/HelpDeskPro.Application/Features/Notifications/Queries/GetNotifications/GetNotificationsQuery.cs` | Baru | Query dan Handler daftar notifikasi user |
| 4 | `src/HelpDeskPro.Application/Features/Notifications/Queries/GetUnreadNotificationCount/GetUnreadNotificationCountQuery.cs` | Baru | Query dan Handler jumlah notifikasi belum dibaca |
| 5 | `src/HelpDeskPro.Application/Features/Notifications/Commands/MarkNotificationsAsRead/MarkNotificationsAsReadCommand.cs` | Baru | Command dan Handler tandai semua notifikasi terbaca |
| 6 | `src/HelpDeskPro.Application/Features/Sla/Commands/CheckSlaBreaches/CheckSlaBreachesCommand.cs` | Baru | Command pemeriksaan pelanggaran SLA & auto-close |
| 7 | `src/HelpDeskPro.Application/Features/Sla/Commands/CheckSlaBreaches/CheckSlaBreachesCommandHandler.cs` | Baru | Handler logika domain SLA response, resolution, idempoten, dan auto-close |
| 8 | `src/HelpDeskPro.Application/Features/Escalations/Models/EscalationDto.cs` | Baru | DTO representasi tiket eskalasi |
| 9 | `src/HelpDeskPro.Application/Features/Tickets/Commands/EscalateTicket/EscalateTicketCommand.cs` | Baru | Command eskalasi manual |
| 10 | `src/HelpDeskPro.Application/Features/Tickets/Commands/EscalateTicket/EscalateTicketCommandValidator.cs` | Baru | Validator catatan eskalasi ($\ge 10$ karakter) |
| 11 | `src/HelpDeskPro.Application/Features/Tickets/Commands/EscalateTicket/EscalateTicketCommandHandler.cs` | Baru | Handler eskalasi manual |
| 12 | `src/HelpDeskPro.Application/Features/Escalations/Commands/AcknowledgeEscalation/AcknowledgeEscalationCommand.cs` | Baru | Command konfirmasi eskalasi |
| 13 | `src/HelpDeskPro.Application/Features/Escalations/Commands/AcknowledgeEscalation/AcknowledgeEscalationCommandHandler.cs` | Baru | Handler konfirmasi eskalasi |
| 14 | `src/HelpDeskPro.Infrastructure/Services/NotificationService.cs` | Baru | Implementasi persisten notifikasi ke database |
| 15 | `src/HelpDeskPro.Infrastructure/Services/SlaMonitorService.cs` | Baru | BackgroundService periodik pemanggil CheckSlaBreaches |
| 16 | `src/HelpDeskPro.Infrastructure/DependencyInjection.cs` | Modifikasi | Registrasi INotificationService & SlaMonitorService |
| 17 | `src/HelpDeskPro.Api/Controllers/EscalationsController.cs` | Baru | Controller konfirmasi eskalasi |
| 18 | `src/HelpDeskPro.Api/Controllers/NotificationsController.cs` | Baru | Controller notifikasi user |
| 19 | `src/HelpDeskPro.Api/Controllers/TicketsController.cs` | Modifikasi | Endpoint eskalasi manual tiket |
| 20 | `src/HelpDeskPro.Application/Features/Tickets/Commands/AssignTicket/AssignTicketCommandHandler.cs` | Modifikasi | Injeksi trigger notifikasi penugasan tiket |
| 21 | `src/HelpDeskPro.Application/Features/Tickets/Commands/AddComment/AddCommentCommandHandler.cs` | Modifikasi | Injeksi trigger notifikasi komentar baru |
| 22 | `src/HelpDeskPro.Application/Features/Tickets/Commands/ChangeTicketStatus/ChangeTicketStatusCommandHandler.cs` | Modifikasi | Injeksi trigger notifikasi perubahan status tiket |
| 23 | `tests/HelpDeskPro.UnitTests/Features/Sla/CheckSlaBreachesCommandHandlerTests.cs` | Baru | Unit test SLA breach, idempoten, pending pause, auto-close |
| 24 | `tests/HelpDeskPro.UnitTests/Features/Escalations/EscalateTicketCommandValidatorTests.cs` | Baru | Unit test validasi eskalasi manual |
| 25 | `tests/HelpDeskPro.UnitTests/Features/Escalations/EscalateTicketCommandHandlerTests.cs` | Baru | Unit test handler eskalasi manual |
| 26 | `tests/HelpDeskPro.UnitTests/Features/Escalations/AcknowledgeEscalationCommandHandlerTests.cs` | Baru | Unit test konfirmasi eskalasi |
| 27 | `tests/HelpDeskPro.UnitTests/Features/Notifications/NotificationTests.cs` | Baru | Unit test get, unread-count, read-all notifikasi |
| 28 | `tests/HelpDeskPro.IntegrationTests/Controllers/TicketsControllerIntegrationTests.cs` | Modifikasi | Integration test eskalasi, acknowledge, dan notifikasi |

