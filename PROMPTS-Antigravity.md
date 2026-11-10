# Paket Prompt Antigravity — HelpDeskPro

## Cara Pakai
1. Buat folder kosong `HelpDeskPro`, buka di Antigravity sebagai workspace.
2. Salin folder `.agent/` dan `docs/` dari paket ini ke root workspace.
3. Jalankan prompt **berurutan**, satu prompt per percakapan agent (Agent Manager → New conversation).
4. Mode: **Planning**. Setujui plan hanya setelah kamu membacanya.
5. Setelah setiap prompt selesai: review diff, jalankan `dotnet build` dan `dotnet test` sendiri, commit, lalu catat di `docs/reports/`.

> Kalau agent menyimpang dari rules, jangan diperbaiki diam-diam. Balas: "Ini melanggar .agent/rules/coding-standard.md bagian X. Perbaiki." Itu melatih kebiasaan review yang dinilai di pekerjaan nyata.

---

## FASE 0 — Fondasi

### P-00 · Setup Solution
```
Baca seluruh file di .agent/rules/ dan docs/FSD-HelpDeskPro.md sebelum mulai.

Tugas: buat kerangka solution HelpDeskPro dengan .NET 8. Belum ada fitur bisnis.

Yang harus dibuat:
1. Solution HelpDeskPro.sln dengan project:
   - src/HelpDeskPro.Domain (classlib)
   - src/HelpDeskPro.Application (classlib)
   - src/HelpDeskPro.Infrastructure (classlib)
   - src/HelpDeskPro.Api (ASP.NET Core Web API, controllers)
   - src/HelpDeskPro.Web (Blazor Web App, Interactive Server)
   - tests/HelpDeskPro.UnitTests (xUnit)
   - tests/HelpDeskPro.IntegrationTests (xUnit)
   Referensi antar project sesuai aturan Clean Architecture di rules.
2. Directory.Build.props: net8.0, Nullable enable, ImplicitUsings enable, TreatWarningsAsErrors true, LangVersion latest, aktifkan .NET analyzers (AnalysisLevel latest-recommended).
3. Directory.Packages.props (Central Package Management) berisi paket: MediatR, FluentValidation, Mapster, EF Core SqlServer + Design, ASP.NET Core Identity EF Core, JWT Bearer, Serilog.AspNetCore, Swashbuckle, MudBlazor, xUnit, FluentAssertions, NSubstitute, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.MsSql, coverlet.collector.
4. .editorconfig yang menegakkan aturan penamaan di rules bagian 3 (private field _camelCase, async suffix, file-scoped namespace) dengan severity warning.
5. docker-compose.yml untuk SQL Server 2022 (port 1433, password dari file .env, sediakan .env.example).
6. .gitignore standar .NET + .env.
7. .github/workflows/ci.yml: restore, build, test dengan coverage pada push dan pull request.
8. README.md: cara menjalankan lokal (docker compose, user secrets, migrate, run Api & Web).

Batasan: jangan buat entity, controller, atau halaman apa pun selain template minimal yang bisa build.
Selesai jika: `dotnet build` tanpa warning dan `dotnet test` lulus (boleh 0 test).
```

### P-01 · Cross-Cutting (Infrastruktur Dasar)
```
Ikuti .agent/rules/coding-standard.md.

Tugas: siapkan komponen lintas-fitur yang akan dipakai semua fitur.

1. Domain:
   - BaseEntity (Id), AuditableEntity (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted).
   - DomainException dan BusinessRuleException.
2. Application:
   - Interface: IDateTimeProvider, ICurrentUserService, IApplicationDbContext.
   - ApiResponse<T> dan PagedResult<T> (Items, Page, PageSize, TotalCount, TotalPages).
   - MediatR pipeline behavior: ValidationBehavior (FluentValidation) dan LoggingBehavior (log nama request + durasi, tanpa isi sensitif).
   - Extension AddApplication() untuk registrasi DI.
3. Infrastructure:
   - AppDbContext (belum ada DbSet bisnis) yang implement IApplicationDbContext.
   - AuditableEntityInterceptor (isi kolom audit otomatis, ubah Delete menjadi soft delete).
   - Global query filter IsDeleted untuk semua AuditableEntity.
   - SystemDateTimeProvider.
   - Extension AddInfrastructure(IConfiguration).
4. Api:
   - Global exception middleware: ValidationException → 400, NotFound → 404, BusinessRuleException → 422, Forbidden → 403, lainnya → 500. Semua dalam format ProblemDetails berisi traceId.
   - Serilog dikonfigurasi dari appsettings (console + file rolling harian).
   - Swagger dengan dukungan Bearer token.
   - CurrentUserService membaca claim dari HttpContext.
5. Unit test: ValidationBehavior (valid lolos, invalid melempar exception dengan pesan), AuditableEntityInterceptor (CreatedAt terisi, delete menjadi soft delete).

Selesai jika build tanpa warning dan semua test lulus. Buat Walkthrough.
```

---

## FASE 1 — Pengembangan v1.0 (.NET 8)

### P-02 · Database & Entity
```
Ikuti rules. Acuan: docs/FSD-HelpDeskPro.md bagian 3, 4, dan 4.8.

Tugas: buat semua entity, konfigurasi EF Core, migration awal, dan seed data.

Entity (Domain) dan tabel:
- Department: Id (int), Code (nvarchar 10, unique), Name (nvarchar 100), IsActive.
- ApplicationUser (extend IdentityUser<Guid>): EmployeeNo (nvarchar 20, unique), FullName (nvarchar 150), DepartmentId (FK), SupervisorId (Guid?, self FK), Role disimpan lewat Identity Role (Requester, Agent, Supervisor, Admin).
- Category: Id, Name (nvarchar 100), IsActive.
- SlaPolicy: Id, Priority (enum TicketPriority, unique), ResponseTimeMinutes, ResolutionTimeMinutes.
- Ticket (AuditableEntity, Id bigint): TicketNo (nvarchar 20, unique), Title (nvarchar 200), Description (nvarchar 4000, null), CategoryId, Priority (tinyint enum), Status (tinyint enum TicketStatus: Open, InProgress, Pending, Resolved, Closed, Cancelled), RequesterId, AssigneeId (null), SlaPolicyId, ResponseDueAt, ResolutionDueAt, FirstRespondedAt (null), ResolvedAt (null), ClosedAt (null), PausedAt (null), TotalPausedMinutes (int default 0), IsSlaBreached (bit), EscalationLevel (tinyint default 0), RowVersion (rowversion).
- TicketComment: Id bigint, TicketId, AuthorId, Body (nvarchar 4000), IsInternal, CreatedAt.
- TicketAttachment: Id bigint, TicketId, FileName, ContentType, FileSizeBytes, StoragePath, UploadedById, UploadedAt.
- TicketHistory: Id bigint, TicketId, FieldName, OldValue (null), NewValue (null), ChangedById, ChangedAt.
- Escalation: Id bigint, TicketId, Level (tinyint), Reason (enum EscalationReason: SlaResponseBreach, SlaResolutionBreach, Manual), Note (nvarchar 1000, null), EscalatedToId, EscalatedById (null untuk sistem), EscalatedAt, AcknowledgedAt (null). Unique index (TicketId, Level, Reason) dengan filter Reason <> Manual.
- Notification: Id bigint, UserId, TicketId (null), Type (enum), Message (nvarchar 500), IsRead, CreatedAt.
- TicketNumberSequence: Year (PK int), LastNumber (int) — untuk FR-04 agar aman dari race condition.

Index tambahan:
- Tickets(Status, Priority)
- Tickets(AssigneeId, Status)
- Tickets(ResolutionDueAt) filter Status IN (Open, InProgress, Pending)
- Notifications(UserId, IsRead)

Semua FK pakai DeleteBehavior.Restrict kecuali Comment/Attachment/History ke Ticket (Cascade).

Seed data (ikuti bagian "Data Contoh" di .agent/rules/ui-design.md):
- 6 departemen, 6 kategori (Hardware, Software, Jaringan, Akses & Akun, Email, Printer).
- 4 SLA sesuai tabel FR-20.
- 12 user: 1 Admin, 2 Supervisor (salah satunya atasan supervisor lain), 4 Agent IT, 5 Requester dari departemen lain. Password dev dari konfigurasi, bukan hardcode.
- 30 tiket dengan variasi status, prioritas, beberapa SLA lewat, beberapa tanpa assignee, beberapa judul panjang, beserta komentar dan history yang masuk akal.
- Seed hanya jalan di environment Development.

Migration bernama "InitialCreate". Buat juga docs/database/data-dictionary.md berisi tabel, kolom, tipe, constraint, dan keterangan, plus ERD dalam Mermaid.

Selesai jika migration berhasil di-apply ke SQL Server docker dan seed masuk.
```

### P-03 · Autentikasi & Otorisasi
```
Ikuti rules. Acuan FSD 3.1, 4.1 (FR-01, FR-02).

1. POST /api/auth/login → kembalikan accessToken (JWT 8 jam), expiresAt, user (id, fullName, email, role, departmentName).
2. Lockout 5x gagal / 15 menit via konfigurasi Identity.
3. GET /api/auth/me.
4. Authorization policies: RequireAgentOrAbove, RequireSupervisorOrAbove, RequireAdmin.
5. Konfigurasi JWT (issuer, audience, key) dari Options pattern + user secrets.
6. Unit test: login sukses, password salah, akun terkunci.
7. Integration test: endpoint dengan token valid 200, tanpa token 401, role kurang 403.

Jangan buat halaman UI di prompt ini.
```

### P-04 · Fitur Buat Tiket
```
Ikuti rules. Acuan FSD 4.2 (FR-03 s.d. FR-06), AC-01, AC-02.

1. CreateTicketCommand + Handler + Validator.
2. ITicketNumberGenerator di Application, implementasi di Infrastructure memakai tabel TicketNumberSequence dengan transaksi + UPDLOCK/ROWLOCK (atau UPDATE ... OUTPUT) agar aman saat request bersamaan. Format HD-{YYYY}-{000000}.
3. Domain method Ticket.Create(...) yang menghitung ResponseDueAt dan ResolutionDueAt dari SlaPolicy dan IDateTimeProvider.
4. Catat TicketHistory "Created".
5. POST /api/tickets → 201 dengan Location header dan ApiResponse<TicketDetailDto>.
6. Unit test minimal: validator (judul kosong, < 5, > 200 karakter, kategori tidak ada), perhitungan due date untuk 4 prioritas, format TicketNo, reset nomor saat ganti tahun.
7. Integration test: 20 request paralel menghasilkan 20 TicketNo unik dan berurutan (AC-02).
```

### P-05 · Daftar & Detail Tiket
```
Ikuti rules. Acuan FSD 3.1 (kolom "Lihat tiket"), 4.3 (FR-07 s.d. FR-09).

1. GetTicketsQuery: filter status (multi), prioritas (multi), kategori, assigneeId, unassigned (bool), dateFrom, dateTo, search (TicketNo atau judul), page, pageSize. Hasil PagedResult<TicketListItemDto>.
2. Hak lihat diterapkan di query (bukan di controller) berdasarkan role user saat ini.
3. Urutan default sesuai FR-08.
4. GetTicketByIdQuery → TicketDetailDto berisi properti, lampiran, dan Timeline (gabungan komentar + history, urut waktu). Komentar internal tidak dikembalikan untuk Requester.
5. Field hitungan: SlaRemainingMinutes (negatif jika lewat), memperhitungkan TotalPausedMinutes.
6. GET /api/tickets dan GET /api/tickets/{id}. Tiket di luar hak akses → 404 (bukan 403) agar tidak bocor keberadaannya.
7. Pastikan query daftar memakai AsNoTracking dan proyeksi (Select/ProjectToType), tidak memuat entity penuh.
8. Unit/integration test: filter, paging, hak lihat 4 role, komentar internal tersembunyi (AC-05).
```

### P-06 · Assign & Alur Status
```
Ikuti rules. Acuan FSD 4.4, 4.5 (FR-10 s.d. FR-15, FR-27), AC-03, AC-07.

1. Implementasikan aturan transisi status sebagai domain logic di entity Ticket (method ChangeStatus), BUKAN di handler. Gunakan tabel transisi yang mudah dibaca.
2. AssignTicketCommand: aturan siapa boleh assign; assign pertama mengisi FirstRespondedAt dan Open → InProgress.
3. ChangeTicketStatusCommand: alasan wajib untuk Pending, catatan wajib untuk Resolved, Requester hanya boleh Cancel (dari Open) dan Close (dari Resolved), reopen maks 7 hari.
4. Pause SLA: masuk Pending isi PausedAt; keluar Pending tambahkan durasi ke TotalPausedMinutes, geser ResolutionDueAt, kosongkan PausedAt.
5. Setiap perubahan mencatat TicketHistory. Konflik RowVersion → 409 dengan pesan "Tiket sudah diubah pengguna lain, muat ulang halaman."
6. Endpoint PATCH /api/tickets/{id}/assign dan PATCH /api/tickets/{id}/status.
7. Unit test: SEMUA kombinasi transisi (valid dan tidak valid) dalam [Theory], pause/resume SLA, aturan per role.
```

### P-07 · Komentar, Lampiran, Ubah Tiket
```
Ikuti rules. Acuan FSD 4.6, 4.7 (FR-16 s.d. FR-19), AC-06.

1. AddCommentCommand: FirstRespondedAt terisi pada komentar pertama dari Agent/Supervisor.
2. Lampiran: IFileStorage di Application, implementasi LocalFileStorage di Infrastructure (folder dari konfigurasi, nama file disimpan sebagai GUID). Validasi jumlah, ukuran, dan ekstensi + cek content type. GET untuk unduh dengan cek hak akses.
3. UpdateTicketCommand (judul, deskripsi, kategori, prioritas) sesuai matriks akses. Perubahan prioritas TIDAK mengubah SLA due date yang sudah ada (catat sebagai keputusan di Walkthrough; jika ragu, tanyakan).
4. History otomatis untuk field yang berubah saja.
5. Endpoint: POST /api/tickets/{id}/comments, POST /api/tickets/{id}/attachments, GET /api/tickets/{id}/attachments/{attachmentId}, PUT /api/tickets/{id}, GET /api/tickets/{id}/history.
6. Test untuk semua validasi dan aturan akses.
```

### P-08 · SLA Monitor, Eskalasi, Notifikasi
```
Ikuti rules. Acuan FSD 4.8, 4.9 (FR-20 s.d. FR-29), AC-04.

1. SlaMonitorService (BackgroundService, interval dari konfigurasi, default 5 menit) memanggil command CheckSlaBreachesCommand. Logic ada di command agar bisa di-unit test tanpa menunggu timer.
2. Aturan FR-22, FR-23, FR-24 (idempoten: jalankan 2x tidak membuat eskalasi ganda).
3. Penentuan penerima eskalasi: Level 1 → SupervisorId milik Assignee, jika belum di-assign → Supervisor departemen IT. Level 2 → SupervisorId milik Supervisor tersebut. Jika penerima tidak ada, log warning dan lanjut.
4. Auto-close FR-15 dijalankan di job yang sama.
5. EscalateTicketCommand (manual, FR-25) dan AcknowledgeEscalationCommand (FR-26).
6. INotificationService: buat Notification untuk kejadian di FR-28. Panggil dari handler terkait (assign, komentar, status, eskalasi).
7. Endpoint: POST /api/tickets/{id}/escalate, PATCH /api/escalations/{id}/acknowledge, GET /api/notifications, GET /api/notifications/unread-count, PATCH /api/notifications/read-all.
8. Unit test dengan IDateTimeProvider palsu: tepat sebelum batas (tidak eskalasi), lewat batas (eskalasi), jalankan dua kali (tetap satu), tiket Pending tidak dihitung, auto-close setelah 3 hari.
```

### P-09 · Dashboard & Master Data
```
Ikuti rules. Acuan FSD 4.10, 4.11 (FR-30 s.d. FR-34).

1. GetDashboardSummaryQuery (parameter periode 7 atau 30 hari): jumlah per status, jumlah SLA lewat, rata-rata menit respons dan resolusi.
2. GetAgentWorkloadQuery dan GetPendingEscalationsQuery.
3. Hasil dibatasi ke departemen Supervisor (Admin melihat semua).
4. CRUD Departemen, Kategori, User (set role, departemen, supervisor, reset password, nonaktifkan). Tidak boleh menghapus data yang masih dipakai → nonaktifkan saja.
5. Update SlaPolicy (FR-34) hanya memengaruhi tiket baru.
6. Endpoint di bawah /api/dashboard/* dan /api/master/*.
7. Pastikan query dashboard efisien (agregasi di SQL, bukan di memori). Sertakan test.
```

### P-10 · UI Blazor — Shell & Tema
```
Baca .agent/rules/ui-design.md dengan teliti. Patuhi semua larangan.

Tugas: buat kerangka UI saja, belum ada halaman fitur.

1. Tema MudBlazor kustom dari token warna, tipografi IBM Plex Sans/Mono, border radius sesuai rules. Simpan di satu file tema.
2. Layout: sidebar 220px (bisa diciutkan) berisi menu sesuai role: Tiket Saya, Semua Tiket, Dashboard, Eskalasi, Master Data. Top bar tipis: judul halaman, pencarian global, lonceng notifikasi dengan jumlah, menu user (nama, role, logout).
3. Halaman login sederhana: form di tengah, tanpa ilustrasi, tanpa gradien.
4. Typed HttpClient ke Api dengan DelegatingHandler yang menyisipkan JWT dan menangani 401 (redirect ke login).
5. Komponen reusable: StatusBadge, PriorityBadge, SlaCountdown, RelativeTime (tooltip waktu WIB), EmptyState (satu kalimat + satu tombol), ConfirmDialog.
6. Shortcut keyboard global: N, /, Esc.

Lampirkan screenshot hasil di Walkthrough (gunakan browser agent untuk membuka localhost).
```

### P-11 · UI — Daftar Tiket
```
Ikuti .agent/rules/ui-design.md. [Lampirkan sketsa/screenshot referensi kamu di sini]

Halaman /tickets:
- Baris filter di atas: Status (multi), Prioritas (multi), Kategori, Assignee, "Belum di-assign", pencarian. Tombol "Buat tiket" (primary) di kanan atas.
- Tabel kolom: No. Tiket (mono, klik untuk salin), Judul (terpotong 1 baris + tooltip), Kategori, Prioritas (badge), Status (badge), Assignee (atau teks abu "Belum di-assign"), Jatuh tempo SLA (SlaCountdown, merah jika lewat), Diperbarui (RelativeTime).
- Header sticky, tinggi baris 40px, hover baris, klik baris membuka detail.
- Paging di bawah dengan pilihan 20/50/100.
- Filter tersimpan per user (localStorage via JS interop, dibungkus try/catch).
- Empty state: "Tidak ada tiket yang cocok dengan filter." + tombol "Reset filter".
- Loading state berupa skeleton baris tabel, bukan spinner besar.

Kerjakan HANYA halaman ini.
```

### P-12 · UI — Detail Tiket & Form Buat Tiket
```
Ikuti .agent/rules/ui-design.md.

1. /tickets/{id}:
   - Header: TicketNo (mono) + judul, tombol aksi kontekstual sesuai role dan status (Ambil tiket, Assign, Ubah status, Eskalasi).
   - Kolom kiri: deskripsi, daftar lampiran, timeline (komentar dan history digabung, history ditampilkan ringkas satu baris abu-abu, komentar internal diberi label "Internal" dengan latar kuning sangat pucat), kotak komentar dengan toggle Internal di bawah.
   - Kolom kanan 320px: Status, Prioritas, Kategori, Assignee, Requester, Dibuat, SLA respons, SLA resolusi (SlaCountdown), Level eskalasi.
   - Dialog ubah status dengan field alasan/catatan wajib sesuai FSD.
   - Tangani 409 konflik dengan pesan dan tombol "Muat ulang".
2. /tickets/new (juga dibuka dengan shortcut N):
   - Form: Judul, Kategori, Prioritas (default Medium, tampilkan teks kecil target SLA sesuai pilihan), Deskripsi, Lampiran (drag-drop + validasi di klien).
   - Setelah sukses, arahkan ke detail tiket dengan toast "Tiket HD-2026-xxxxxx dibuat".
```

### P-13 · UI — Dashboard, Eskalasi, Master Data, Notifikasi
```
Ikuti .agent/rules/ui-design.md, terutama larangan dashboard kartu statistik besar.

1. /dashboard (Supervisor/Admin): baris ringkasan angka sederhana (teks, bukan kartu besar), tabel beban kerja Agent, tabel eskalasi belum ditindaklanjuti dengan tombol "Tandai ditindaklanjuti", satu grafik batang sederhana jumlah tiket masuk vs selesai per hari (MudChart, warna netral + aksen primary).
2. /escalations: daftar eskalasi ke saya.
3. /admin/*: halaman Departemen, Kategori, User, SLA dengan tabel + dialog form.
4. Panel notifikasi dari ikon lonceng (popover, maks 10 terbaru, link "Lihat semua").
```

### P-14 · Hardening & Release v1.0
```
Ikuti rules.

1. Jalankan seluruh test, laporkan coverage per project (coverlet + reportgenerator). Tambah test hingga Application ≥ 70%.
2. Review mandiri seluruh kode terhadap .agent/rules/coding-standard.md. Buat daftar pelanggaran yang ditemukan dan perbaiki.
3. Cek N+1 query di endpoint daftar dan detail (aktifkan logging SQL sementara).
4. Pastikan tidak ada secret di repo.
5. Buat docs/release-notes/v1.0.md (fitur, known issues, cara deploy) dan tag versi 1.0.0.
```

---

## FASE 2 — Modify Existing (Change Request)

> Sebelum P-15, buat file docs/CR/CR-001.md, CR-002.md, CR-003.md dari teks di bawah. Ini mensimulasikan perubahan kebutuhan dari user.

### P-15 · CR-001 Rating Kepuasan
```
Ikuti rules. Baca docs/CR/CR-001.md dan kode yang sudah ada sebelum membuat plan.

CR-001: Setelah tiket Resolved, Requester dapat memberi rating 1–5 dan komentar opsional (maks 500 karakter) satu kali saja. Memberi rating otomatis menutup tiket (Closed). Dashboard menampilkan rata-rata rating per Agent pada periode terpilih.

Aturan modifikasi:
- Analisis dampak dulu: file mana yang berubah, risiko regresi, migration baru apa. Tulis di plan.
- Buat migration BARU (jangan ubah InitialCreate).
- Semua test lama harus tetap lulus tanpa diubah, kecuali kamu menjelaskan alasannya di plan dan saya setujui.
- Tambahkan test untuk perilaku baru.
- Update docs/database/data-dictionary.md dan tambahkan entri di changelog.
```

### P-16 · CR-002 SLA Jam Kerja
```
Ikuti rules. Baca docs/CR/CR-002.md.

CR-002: Perhitungan SLA hanya menghitung jam kerja: Senin–Jumat 08.00–17.00 WIB. Hari libur nasional dikelola Admin (tabel baru Holidays: Date, Name). Contoh: tiket Medium (24 jam kerja) dibuat Jumat 16.00 WIB → jatuh tempo dihitung melewati Sabtu-Minggu.

Aturan:
- Ekstrak logic ke IBusinessCalendar / ISlaCalculator yang bisa diganti; jangan tebar logic baru di banyak handler.
- Tiket yang sudah ada TIDAK dihitung ulang. Tulis keputusan ini di ADR docs/ADR/002-business-hours-sla.md.
- Unit test wajib mencakup: dibuat di jam kerja, sebelum jam 08.00, setelah 17.00, Jumat sore melewati akhir pekan, melewati hari libur, Pending yang melewati malam hari.
- Semua test SLA lama yang memakai jam kalender dinyatakan di plan: diubah atau dipertahankan, dengan alasan.
- CRUD hari libur di halaman Admin.
```

### P-17 · CR-003 Upgrade ke .NET 10
```
Ikuti rules. Baca docs/CR/CR-003.md.

CR-003: Upgrade seluruh solution dari .NET 8 ke .NET 10 (LTS).

Langkah yang saya harapkan di plan:
1. Inventaris: versi semua package saat ini dan versi yang kompatibel dengan net10.0. Tandai yang belum mendukung.
2. Daftar breaking changes .NET 9 dan .NET 10 yang relevan dengan kode ini (ASP.NET Core, EF Core, Blazor, Identity). Sebutkan sumbernya.
3. Ubah TargetFramework di Directory.Build.props, update Directory.Packages.props, update global.json dan CI.
4. Perbaiki error dan warning build satu per satu; jelaskan setiap perbaikan di Walkthrough.
5. Jalankan seluruh test unit dan integration; semua harus hijau.
6. Smoke test manual lewat browser agent: login, buat tiket, ubah status, dashboard. Lampirkan screenshot.

Jika ada paket yang belum mendukung .NET 10, BERHENTI dan laporkan opsi (tunggu, ganti paket, atau pertahankan versi lama) beserta risikonya. Jangan memilih sendiri — ini harus dieskalasi ke supervisor.

Akhiri dengan docs/release-notes/v1.1.md.
```

---

## FASE 3 — Dokumentasi & Pelaporan

### P-18 · Technical Specification Document
```
Baca seluruh kode, docs/FSD-HelpDeskPro.md, docs/CR/, docs/ADR/, dan walkthrough sebelumnya.

Tulis docs/TSD-HelpDeskPro.md dengan struktur baku:
1. Document Control (versi, tanggal, author, reviewer, riwayat revisi)
2. Pendahuluan, ruang lingkup, referensi (FSD, CR)
3. Arsitektur sistem: diagram layer (Mermaid), alasan Clean Architecture, daftar ADR
4. Desain database: ERD (Mermaid), ringkasan tabel, link ke data-dictionary
5. Desain API: tabel endpoint (method, path, role, request, response, error code), format ApiResponse dan ProblemDetails
6. Desain modul: untuk Create Ticket, Ubah Status, SLA Monitor & Eskalasi → sequence diagram (Mermaid) + business rule + referensi FR
7. Keamanan: autentikasi, matriks otorisasi, penanganan file upload, secret management
8. Logging, monitoring, error handling
9. Strategi testing + tabel test case (ID, FR terkait, skenario, langkah, expected, status) minimal untuk semua AC di FSD
10. Konfigurasi & deployment: appsettings, environment variable, langkah deploy
11. Change log v1.0 → v1.1 (CR-001, CR-002, CR-003)
12. Known issues & rekomendasi

Gunakan bahasa Indonesia formal. Setiap klaim harus sesuai kode yang ada — jika kode dan FSD berbeda, tulis di bagian Known issues, jangan disamakan diam-diam.
```

### P-19 · Laporan Mingguan (pakai setiap Jumat)
```
Berdasarkan git log minggu ini (dari {tanggal} s.d. {tanggal}), task list, dan walkthrough:

Buat docs/reports/weekly-{YYYY}-W{nn}.md dengan format:
- Ringkasan (2–3 kalimat)
- Progress vs rencana (tabel: item, rencana, aktual, status On track/At risk/Delayed)
- Selesai minggu ini
- Rencana minggu depan
- Issue & risiko (tabel: ID, deskripsi, dampak, mitigasi, perlu eskalasi? Ya/Tidak, ke siapa)
- Keputusan yang dibutuhkan dari supervisor

Tulis faktual dan ringkas. Jangan melebih-lebihkan progress.
```

---

## Prompt Darurat (dipakai saat diperlukan)

**Agent menyimpang dari rules**
```
Hentikan. Perubahan terakhir melanggar .agent/rules/{file} bagian {X}: {jelaskan}. Kembalikan ke kondisi sesuai rules tanpa mengubah perilaku lain, lalu jelaskan apa yang diperbaiki.
```

**Build/test gagal dan agent berputar-putar**
```
Berhenti mencoba perbaikan. Tampilkan pesan error lengkap, sebutkan 2–3 kemungkinan penyebab beserta bukti dari kode, dan usulkan satu perbaikan terkecil. Jangan ubah kode sampai saya setuju.
```

**Review kode sebelum commit**
```
Review diff yang belum di-commit sebagai senior .NET reviewer. Periksa: kepatuhan rules, bug logika, N+1 query, masalah keamanan, test yang kurang. Berikan temuan dalam tabel (file, baris, tingkat: Blocker/Major/Minor, saran). Jangan ubah kode.
```

**UI terlihat "AI banget"**
```
Bandingkan halaman {nama} dengan .agent/rules/ui-design.md. Daftar setiap pelanggaran (gradien, emoji, kartu yang seharusnya tabel, radius, spasi, copywriting, data seed tidak realistis), lalu perbaiki satu per satu. Lampirkan screenshot sebelum dan sesudah.
```
