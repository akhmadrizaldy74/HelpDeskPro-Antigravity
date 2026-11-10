# Functional Specification Document — HelpDeskPro

| Item | Isi |
|---|---|
| Versi | 1.0 |
| Tanggal | 9 November 2026 |
| Status | Approved for development |
| Target platform | .NET 8 (v1.0), upgrade ke .NET 10 (v1.1) |

---

## 1. Latar Belakang
Permintaan bantuan IT di perusahaan saat ini masuk lewat WhatsApp dan email, sehingga sulit dilacak, tidak ada prioritas yang jelas, dan keterlambatan penanganan tidak terpantau. HelpDeskPro adalah aplikasi web internal untuk mencatat, menugaskan, memantau, dan mengeskalasi tiket permintaan IT berdasarkan SLA.

## 2. Ruang Lingkup v1.0
**Termasuk:** login & role, master data, pembuatan dan pengelolaan tiket, komentar, lampiran, riwayat perubahan, SLA, eskalasi otomatis dan manual, notifikasi dalam aplikasi, dashboard supervisor.

**Tidak termasuk:** integrasi email masuk (email-to-ticket), aplikasi mobile, knowledge base, SSO Active Directory.

## 3. Pengguna & Role
| Role | Deskripsi |
|---|---|
| Requester | Karyawan yang membuat tiket |
| Agent | Staf IT yang menangani tiket |
| Supervisor | Atasan Agent, menerima eskalasi, memantau dashboard |
| Admin | Mengelola user dan master data |

### 3.1 Matriks Akses
| Fitur | Requester | Agent | Supervisor | Admin |
|---|---|---|---|---|
| Buat tiket | Ya | Ya | Ya | Ya |
| Lihat tiket | Milik sendiri | Di-assign ke dirinya + belum di-assign | Semua di departemennya | Semua |
| Ubah judul/deskripsi | Milik sendiri, status Open | Tidak | Ya | Ya |
| Ubah status | Batalkan (Open saja), Tutup (Resolved saja) | Ya | Ya | Ya |
| Assign tiket | Tidak | Ambil sendiri (self-assign) | Ya | Ya |
| Komentar publik | Ya | Ya | Ya | Ya |
| Komentar internal | Tidak (tidak terlihat) | Ya | Ya | Ya |
| Eskalasi manual | Tidak | Ya | Ya | Ya |
| Dashboard | Tidak | Tidak | Ya | Ya |
| Master data & user | Tidak | Tidak | Tidak | Ya |

## 4. Kebutuhan Fungsional

### 4.1 Login
- FR-01: User login dengan email dan password. Sistem mengembalikan JWT (berlaku 8 jam).
- FR-02: Gagal login 5 kali berturut-turut mengunci akun selama 15 menit.

### 4.2 Buat Tiket
- FR-03: Input: Judul (wajib, 5–200 karakter), Deskripsi (opsional, maks 4000), Kategori (wajib), Prioritas (wajib, default Medium), Lampiran (opsional).
- FR-04: Sistem membuat `TicketNo` otomatis format `HD-{YYYY}-{6 digit urut per tahun}`, contoh `HD-2026-000123`. Nomor urut reset tiap tahun dan tidak boleh duplikat walaupun dibuat bersamaan.
- FR-05: Sistem mengambil SLA berdasarkan Prioritas, lalu mengisi `ResponseDueAt = CreatedAt + ResponseTimeMinutes` dan `ResolutionDueAt = CreatedAt + ResolutionTimeMinutes`.
- FR-06: Status awal `Open`. Riwayat "Tiket dibuat" dicatat di history.

### 4.3 Daftar & Detail Tiket
- FR-07: Daftar tiket dengan filter status, prioritas, kategori, assignee, rentang tanggal, dan pencarian teks (TicketNo/judul). Paging default 20.
- FR-08: Urutan default: tiket SLA lewat paling atas, lalu `ResolutionDueAt` terdekat.
- FR-09: Detail menampilkan properti tiket, deskripsi, lampiran, timeline gabungan komentar dan history berurutan waktu, serta sisa waktu SLA.

### 4.4 Assign
- FR-10: Supervisor/Admin dapat assign ke Agent mana pun di departemen IT. Agent dapat mengambil tiket yang belum di-assign.
- FR-11: Assign pertama kali mengisi `FirstRespondedAt` jika belum terisi, dan otomatis mengubah status Open → InProgress.

### 4.5 Alur Status
```
Open ──► InProgress ◄──► Pending
  │           │
  │           ▼
  │       Resolved ──► Closed
  │           │
  │           └──► InProgress (dibuka ulang, maks 7 hari setelah Resolved)
  ▼
Cancelled
```
- FR-12: Transisi di luar diagram ditolak dengan pesan "Perubahan status dari {A} ke {B} tidak diizinkan."
- FR-13: Pindah ke `Pending` wajib menyertakan alasan (disimpan sebagai komentar internal).
- FR-14: Pindah ke `Resolved` mengisi `ResolvedAt` dan wajib menyertakan catatan penyelesaian.
- FR-15: Tiket `Resolved` otomatis menjadi `Closed` setelah 3 hari tanpa respons Requester.

### 4.6 Komentar & Lampiran
- FR-16: Komentar maks 4000 karakter, bisa ditandai Internal (hanya Agent/Supervisor/Admin yang melihat).
- FR-17: Komentar pertama dari Agent mengisi `FirstRespondedAt` jika belum terisi.
- FR-18: Lampiran: maks 5 file per tiket, maks 5 MB per file, tipe yang diizinkan: jpg, png, pdf, docx, xlsx, txt, log.

### 4.7 Riwayat (History)
- FR-19: Setiap perubahan Status, Prioritas, Assignee, Kategori, Judul dicatat: nama field, nilai lama, nilai baru, oleh siapa, kapan.

### 4.8 SLA & Eskalasi
- FR-20: Tabel SLA default:

| Prioritas | Respons | Resolusi |
|---|---|---|
| Critical | 30 menit | 4 jam |
| High | 2 jam | 8 jam |
| Medium | 4 jam | 24 jam |
| Low | 8 jam | 72 jam |

- FR-21: Background job berjalan tiap 5 menit memeriksa tiket berstatus Open/InProgress/Pending.
- FR-22: Jika `now > ResponseDueAt` dan `FirstRespondedAt` kosong → buat eskalasi Level 1 ke Supervisor dari Agent (atau Supervisor departemen IT jika belum di-assign), alasan `SlaResponseBreach`.
- FR-23: Jika `now > ResolutionDueAt` dan belum Resolved → set `IsSlaBreached = true`, buat eskalasi Level 2 ke atasan Supervisor, alasan `SlaResolutionBreach`.
- FR-24: Eskalasi dengan level dan alasan yang sama untuk satu tiket hanya dibuat sekali.
- FR-25: Agent/Supervisor dapat eskalasi manual dengan alasan teks wajib (min 10 karakter).
- FR-26: Penerima eskalasi dapat menandai "Sudah ditindaklanjuti" (mengisi `AcknowledgedAt`).
- FR-27: Selama status `Pending`, waktu SLA resolusi tidak berjalan (jam dihentikan dan dilanjutkan saat kembali ke InProgress).

### 4.9 Notifikasi
- FR-28: Notifikasi dalam aplikasi untuk: tiket di-assign ke saya, komentar baru di tiket saya, status tiket saya berubah, eskalasi ke saya.
- FR-29: Ikon lonceng di top bar menampilkan jumlah belum dibaca; bisa tandai semua sudah dibaca.

### 4.10 Dashboard Supervisor
- FR-30: Ringkasan: jumlah tiket per status, jumlah SLA lewat, rata-rata waktu respons & resolusi (7/30 hari).
- FR-31: Tabel beban kerja per Agent: jumlah tiket aktif, jumlah SLA lewat.
- FR-32: Daftar eskalasi yang belum ditindaklanjuti.

### 4.11 Master Data (Admin)
- FR-33: CRUD Departemen, Kategori, User (termasuk set role dan supervisor).
- FR-34: Ubah nilai SLA per prioritas. Perubahan hanya berlaku untuk tiket baru.

## 5. Kebutuhan Non-Fungsional
- NFR-01: Waktu respons API daftar tiket < 500 ms untuk 50.000 tiket.
- NFR-02: Semua waktu disimpan UTC, ditampilkan WIB.
- NFR-03: Password di-hash (ASP.NET Core Identity).
- NFR-04: Log terstruktur dengan TraceId per request.
- NFR-05: Coverage unit test layer Application ≥ 70%.
- NFR-06: Aplikasi dapat dijalankan lokal dengan satu perintah `docker compose up` untuk database.

## 6. Kriteria Penerimaan (contoh)
| ID | Skenario | Hasil yang Diharapkan |
|---|---|---|
| AC-01 | Requester membuat tiket prioritas High | TicketNo terbentuk, status Open, ResponseDueAt = +2 jam |
| AC-02 | Dua tiket dibuat bersamaan | TicketNo berbeda dan berurutan |
| AC-03 | Agent mengubah Open → Resolved langsung | Ditolak dengan pesan transisi tidak diizinkan |
| AC-04 | Tiket Critical tidak direspons 31 menit | Eskalasi Level 1 terbentuk satu kali, Supervisor dapat notifikasi |
| AC-05 | Requester membuka detail tiket | Komentar internal tidak tampil |
| AC-06 | Upload file .exe | Ditolak dengan pesan tipe file tidak diizinkan |
| AC-07 | Tiket Pending 2 jam lalu kembali InProgress | ResolutionDueAt bergeser +2 jam |
