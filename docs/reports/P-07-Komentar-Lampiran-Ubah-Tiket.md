# Laporan Implementasi — P-07 Komentar, Lampiran, Ubah Tiket

| Item | Isi |
|---|---|
| Modul | Komentar, Lampiran, Ubah Tiket, & Riwayat (FR-16 s.d. FR-19, AC-06) |
| Tanggal | 10 November 2026 |
| Target Framework | .NET 8 |
| Status | Selesai & Lulus Pengujian (236/236 Tests Passed) |

---

## 1. Ringkasan Implementasi

Pada tahap **P-07 · Komentar, Lampiran, Ubah Tiket**, kami telah mengimplementasikan fitur komentar, manajemen lampiran file fisik, pengubahan detail tiket dengan pencatatan riwayat selektif, serta endpoint riwayat tiket sesuai FSD 3.1, 4.6, 4.7 (FR-16 s.d. FR-19), dan AC-06:

1. **Fitur Komentar (`Ticket.AddComment` & `AddCommentCommand`)**:
   - Validasi: isi komentar wajib diisi dan maksimal 4000 karakter (**FR-16**).
   - Otorisasi matriks 3.1: Requester hanya dapat membuat komentar publik. Jika Requester mencoba membuat komentar internal (`IsInternal = true`), sistem menolak melempar `BusinessRuleException` (*"Requester tidak diizinkan membuat komentar internal."*).
   - First Response (**FR-17**): Komentar pertama yang dikirimkan oleh pengguna dengan role `Agent`, `Supervisor`, atau `Admin` secara otomatis mengisi timestamp `FirstRespondedAt` jika sebelumnya masih kosong (`null`). Komentar dari Requester tidak memicu pengisian ini.

2. **Penyimpanan File & Lampiran Tiket (`IFileStorage`, `LocalFileStorage`, Upload, Download)**:
   - **Abstraksi `IFileStorage`** di layer Application dan implementasi **`LocalFileStorage`** di layer Infrastructure:
     - Direktori penyimpanan dibaca dari konfigurasi `FileStorage:BasePath` (default: folder `uploads`).
     - Nama file fisik dienkapsulasi menggunakan GUID (`{Guid}{extension}`) untuk mencegah benturan nama dan melindungi sistem dari celah keamanan *path traversal*.
   - **Validasi Lampiran (FR-18, AC-06)**:
     - Kuota per tiket: maksimal 5 file. Melebihi kuota ditolak dengan `BusinessRuleException` (*"Jumlah lampiran untuk tiket ini sudah mencapai batas maksimal (5 file)."*).
     - Ukuran per file: maksimal 5 MB (`5 * 1024 * 1024` byte). Melebihi batas ditolak (*"Ukuran file melebihi batas maksimal 5 MB."*).
     - Whitelist ekstensi: `.jpg`, `.jpeg`, `.png`, `.pdf`, `.docx`, `.xlsx`, `.txt`, `.log`.
     - Validasi file eksekutabel (**AC-06**): file `.exe`, `.bat`, `.cmd`, `.sh`, `.zip`, atau ekstensi di luar whitelist ditolak dengan pesan validasi exact:
       `"Tipe file tidak diizinkan."`
   - **Download Lampiran (`DownloadTicketAttachmentQuery`)**:
     - `GET /api/tickets/{id}/attachments/{attachmentId}`.
     - Memeriksa hak akses user terhadap tiket sebelum mengalirkan (*stream*) isi file fisik.

3. **Ubah Tiket (`Ticket.UpdateDetails` & `UpdateTicketCommand`)**:
   - Memperbarui: `Title`, `Description`, `CategoryId`, dan `Priority`.
   - Otorisasi matriks akses FSD 3.1:
     - **Requester**: hanya dapat mengubah tiket milik sendiri dan HANYA saat status tiket masih `Open`. Jika status tiket sudah `InProgress`, `Pending`, dsb, ditolak (*"Requester hanya dapat mengubah tiket saat status masih Open."*).
     - **Agent**: tidak memiliki wewenang mengubah judul/deskripsi/detail tiket (*"Agent tidak memiliki wewenang untuk mengubah detail tiket."*).
     - **Supervisor**: dapat mengubah tiket di departemennya atau departemen IT.
     - **Admin**: dapat mengubah semua tiket.
   - **Keputusan Desain SLA Due Date**:
     - Perubahan prioritas pada tiket yang sudah berjalan **TIDAK mengubah** `ResponseDueAt` maupun `ResolutionDueAt` yang telah dihitung saat tiket pertama kali dibuat.
   - **Pencatatan Riwayat Selektif (FR-19)**:
     - Sistem mendeteksi perbedaan nilai lama vs baru secara presisi dan HANYA mencatat entri `TicketHistory` untuk atribut yang benar-benar berubah (`Title`, `Description`, `Category`, atau `Priority`). Jika prioritas tidak berubah misalnya, entri riwayat untuk `Priority` tidak akan dibuat.
   - Optimistic Concurrency Control: request wajib menyertakan `RowVersion`.

4. **Riwayat Tiket (`GetTicketHistoryQuery`)**:
   - `GET /api/tickets/{id}/history`.
   - Mengembalikan daftar riwayat perubahan tiket yang diurutkan secara kronologis berdasarkan waktu (`ChangedAt` ascending).

5. **API Endpoints (`TicketsController`)**:
   - `POST /api/tickets/{id}/comments`: membuat komentar baru (201 Created).
   - `POST /api/tickets/{id}/attachments`: mengunggah lampiran multipart/form-data (201 Created).
   - `GET /api/tickets/{id}/attachments/{attachmentId}`: mengunduh file lampiran (200 OK FileStream).
   - `PUT /api/tickets/{id}`: memperbarui judul, deskripsi, kategori, dan prioritas (200 OK).
   - `GET /api/tickets/{id}/history`: mengambil daftar riwayat perubahan tiket (200 OK).

6. **Pengujian Menyeluruh (236/236 Tests Passed)**:
   - **216 Unit Tests**:
     - `LocalFileStorageTests`: simpan, baca stream, dan hapus file.
     - `AddCommentCommandValidatorTests` & `AddCommentCommandHandlerTests`: validasi panjang komentar, pengisian `FirstRespondedAt` oleh Agent/Supervisor (**FR-17**), larangan komentar internal bagi Requester.
     - `UploadTicketAttachmentCommandValidatorTests` & `UploadTicketAttachmentCommandHandlerTests`: penolakan file `.exe` (**AC-06**), batas 5 file, batas 5 MB, dan ekstensi whitelist.
     - `DownloadTicketAttachmentQueryHandlerTests`: verifikasi stream dan penanganan file hilang.
     - `UpdateTicketCommandValidatorTests` & `UpdateTicketCommandHandlerTests`: aturan akses per role, pembatasan status Open untuk Requester, pencegahan mutasi SLA due date pada perubahan prioritas, dan pencatatan history selektif (**FR-19**).
     - `GetTicketHistoryQueryHandlerTests`: pengembalian riwayat urut waktu.
   - **20 Integration Tests**:
     - `POST /api/tickets/{id}/comments` oleh Agent mengisi `FirstRespondedAt`.
     - `POST /api/tickets/{id}/attachments` dengan file `.exe` ditolak 400 Bad Request dengan pesan *"Tipe file tidak diizinkan."* (**AC-06**).
     - `POST /api/tickets/{id}/attachments` dengan file `.png` berhasil (201 Created).
     - `GET /api/tickets/{id}/attachments/{attachmentId}` mengalirkan konten file (200 OK).
     - `PUT /api/tickets/{id}` oleh Requester pada tiket InProgress ditolak (422 UnprocessableEntity).
     - `PUT /api/tickets/{id}` oleh Admin berhasil dan memperbarui data.
     - `GET /api/tickets/{id}/history` mengembalikan riwayat perubahan.
   - Code Coverage:
     - `HelpDeskPro.Application`: **87.77%** (target $\ge 70\%$).
     - `HelpDeskPro.Domain`: **85.08%** (target $\ge 70\%$).

---

## 2. Struktur File yang Dibuat & Diperbarui

| Layer | File | Keterangan |
|---|---|---|
| Domain | `Entities/Ticket.cs` | Penambahan method `AddComment(...)`, `AddAttachment(...)`, dan `UpdateDetails(...)` |
| Application | `Common/Interfaces/IFileStorage.cs` | Abstraksi penyimpanan file fisik |
| Application | `Features/Tickets/Models/TicketCommentDto.cs` | DTO respon komentar |
| Application | `Features/Tickets/Models/TicketHistoryDto.cs` | DTO respon riwayat tiket |
| Application | `Features/Tickets/Commands/AddComment/*` | Command, Validator, Handler untuk penambahan komentar |
| Application | `Features/Tickets/Commands/UploadTicketAttachment/*` | Command, Validator, Handler untuk upload lampiran |
| Application | `Features/Tickets/Queries/DownloadTicketAttachment/*` | Query, DTO, Handler untuk download lampiran |
| Application | `Features/Tickets/Commands/UpdateTicket/*` | Command, Validator, Handler untuk pengubahan tiket |
| Application | `Features/Tickets/Queries/GetTicketHistory/*` | Query, Handler untuk mengambil riwayat tiket |
| Infrastructure | `Services/LocalFileStorage.cs` | Implementasi penyimpanan file lokal dengan penamaan GUID |
| Infrastructure | `DependencyInjection.cs` | Registrasi `IFileStorage` ke service collection |
| Api | `Middlewares/GlobalExceptionMiddleware.cs` | Perbaikan serialisasi polimorfik untuk `ValidationProblemDetails` |
| Api | `Controllers/TicketsController.cs` | Endpoint comments, attachments (upload & download), update, dan history |
| Tests | `UnitTests/Common/LocalFileStorageTests.cs` | Unit tests untuk penyimpanan file |
| Tests | `UnitTests/Features/Tickets/AddComment*Tests.cs` | Unit tests komentar & FirstRespondedAt |
| Tests | `UnitTests/Features/Tickets/UploadTicketAttachment*Tests.cs` | Unit tests upload lampiran & AC-06 |
| Tests | `UnitTests/Features/Tickets/DownloadTicketAttachment*Tests.cs` | Unit tests download lampiran |
| Tests | `UnitTests/Features/Tickets/UpdateTicket*Tests.cs` | Unit tests update tiket, selective history, & SLA policy |
| Tests | `UnitTests/Features/Tickets/GetTicketHistoryQueryHandlerTests.cs` | Unit tests pembacaan riwayat tiket |
| Tests | `IntegrationTests/Controllers/TicketsControllerIntegrationTests.cs` | Integration tests end-to-end seluruh endpoint P-07 |

