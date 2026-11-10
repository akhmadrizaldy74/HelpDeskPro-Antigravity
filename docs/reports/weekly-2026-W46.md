# Laporan Mingguan Tim Pengembang (Weekly Report)
**Periode:** Minggu ke-46 (09 November 2026 – 15 November 2026)  
**Proyek:** HelpDeskPro  
**Target Rilis:** v1.0.0 (Baseline) $\rightarrow$ v1.1.0 (Modernisasi & CR)  
**Status Keseluruhan:** On Track  

---

## 1. Ringkasan Eksekutif
Dalam periode 09 s.d. 11 November 2026, seluruh target pengembangan berhasil diselesaikan tepat waktu, diawali dengan rilis resmi HelpDeskPro v1.0.0 (354 tests, cakupan aplikasi 86,6%) yang kemudian dilanjutkan dengan penyelesaian dua *Change Request* bisnis (CR-001 Rating Kepuasan dan CR-002 SLA Jam Kerja). Selain itu, solusi telah berhasil dimodernisasi secara penuh ke **.NET 10 LTS** (CR-003) dengan penegakan kompilasi ketat (`0 Warning, 0 Error`) serta seluruh 376 pengujian otomatis berstatus 100% lulus (*green*). Dokumen teknis komprehensif (*Technical Specification Document* — TSD) dan catatan rilis v1.1.0 telah diterbitkan dan di-tag ke repositori.

---

## 2. Progress vs Rencana

| Item Pekerjaan | Target Rencana | Realisasi Aktual | Status |
|---|---|---|:---:|
| **Hardening & Rilis Baseline v1.0.0** | Selesai 10 Nov 2026 | Selesai 10 Nov 2026 (Tag `1.0.0`, release notes v1.0 diterbitkan) | **On Track** |
| **CR-001: Rating Kepuasan Layanan** | Selesai 10 Nov 2026 | Selesai 10 Nov 2026 (Rating 1–5, auto-close, metrik agent, migrasi EF) | **On Track** |
| **CR-002: Perhitungan SLA Jam Kerja & Hari Libur** | Selesai 11 Nov 2026 | Selesai 11 Nov 2026 (Jam kerja 08.00–17.00 WIB, tabel Holidays, ADR-002) | **On Track** |
| **CR-003: Upgrade Solusi ke .NET 10 LTS** | Selesai 11 Nov 2026 | Selesai 11 Nov 2026 (SDK 10.0.404, 22 paket NuGet, MudBlazor v9, 376 tests lulus) | **On Track** |
| **Penyusunan Technical Specification Document (TSD)** | Selesai 11 Nov 2026 | Selesai 11 Nov 2026 (`docs/TSD-HelpDeskPro.md` lengkap 12 bab) | **On Track** |

---

## 3. Selesai Minggu Ini

Berdasarkan riwayat git log minggu ini:
1. **Rilis HelpDeskPro v1.0.0 (`504e8a2`, `9d10286`)**:
   - Menuntaskan seluruh 34 kebutuhan fungsional (FR-01 s.d. FR-34) mencakup autentikasi JWT, siklus tiket, timeline komentar/lampiran, audit trail, background worker SLA monitor, dashboard analitik, dan administrasi master data.
   - Pembersihan *technical debt*: audit secret repo, eliminasi N+1 query, dan peningkatan test coverage Application hingga 86,6%.
2. **Implementasi CR-001 — Rating Kepuasan (`a4131b6`)**:
   - Penambahan kolom `Rating`, `RatingComment`, dan `RatedAt` pada entitas tiket melalui migrasi `20261110163426_AddTicketRating`.
   - Endpoint `POST /api/tickets/{id}/rate` yang secara atomik mengubah status tiket dari `Resolved` menjadi `Closed`.
   - Tampilan bintang rating dan integrasi metrik rata-rata kepuasan per teknisi pada Dashboard Supervisor.
3. **Implementasi CR-002 — SLA Jam Kerja (`eb4ff1c`)**:
   - Sentralisasi kalkulasi jam kerja operasional (Senin–Jumat 08.00–17.00 WIB) via abstraksi `IBusinessCalendar` dan `ISlaCalculator`.
   - Entitas dan tabel baru `Holidays` untuk pengelolaan tanggal libur nasional via antarmuka admin Blazor.
   - Dokumentasi keputusan arsitektur **ADR-002**: tiket lama tidak dihitung ulang demi integritas audit historis.
4. **Implementasi CR-003 — Upgrade .NET 10 LTS (`7ae4ca1`)**:
   - Migrasi 7 proyek solusi ke `net10.0` dan pembaruan 22 paket dependensi terpusat di `Directory.Packages.props`.
   - Persetujuan eskalasi paket `FluentValidation 12.2.0` via backward compatibility .NET 8 $\rightarrow$ .NET 10 (Opsi A).
   - Adaptasi breaking changes: MediatR 14 `CancellationToken`, OpenAPI 2.0 namespace & security schemes, MudBlazor v9 (`IMudDialogInstance`, generic `ChartSeries<double>`, token tipografi CSS string).
   - Eksekusi pengujian: 376 tests lulus 100% (339 unit tests, 37 integration tests) dan penerbitan tag git `1.1.0`.
5. **Penyusunan Dokumentasi Teknis Sistem (`736d74b`)**:
   - Penerbitan dokumen arsitektur komprehensif `docs/TSD-HelpDeskPro.md` yang memuat diagram Mermaid, ERD, kamus API, sekuriti, strategi testing, dan catatan perbedaan FSD.

---

## 4. Rencana Minggu Depan

1. **UAT (User Acceptance Testing) v1.1.0**:
   - Melakukan sesi pengujian terpandu bersama perwakilan pengguna (Requester, Agent, Supervisor IT, dan Admin).
   - Pengujian skenario khusus: alur rating kepuasan (CR-001) dan akurasi batas SLA melompati akhir pekan/hari libur (CR-002).
2. **Persiapan Staging Deployment**:
   - Deployment aplikasi pada server staging berbasis container Docker dengan database SQL Server 2022 terkelola.
   - Verifikasi performa endpoint daftar tiket terhadap volume data simulasi skala besar (NFR-01).
3. **Eksplorasi Fitur Notifikasi Real-Time (SignalR)**:
   - Membuat *Proof of Concept* (PoC) penambahan SignalR Hub pada Web API untuk mendorong notifikasi tiket langsung ke browser tanpa jeda polling.

---

## 5. Isu & Risiko

| ID | Deskripsi Isu / Risiko | Dampak | Rencana Mitigasi | Perlu Eskalasi? | Tindak Lanjut Ke |
|:---:|---|---|---|:---:|---|
| **RSK-01** | Paket `FluentValidation 12.2.0` belum memiliki native TFM `net10.0` (berjalan via .NET 8 compatibility asset). | Rendah. Tidak ada warning build atau kegagalan fungsional pada runtime saat ini. | Menjadwalkan evaluasi pembaruan saat rilis major FluentValidation native .NET 10 tersedia resmi. | **Tidak** (Sudah disetujui via Opsi A) | Supervisor / Tech Lead |
| **RSK-02** | Lingkungan developer lokal tanpa daemon Docker aktif memerlukan konfigurasi database manual saat menjalankan tes integrasi non-in-memory. | Rendah. Test suite saat ini telah dilengkapi test harness terisolasi. | Menyediakan dokumentasi panduan koneksi local SQL Server Express / LocalDB di dokumen TSD. | **Tidak** | Lead Developer |
| **RSK-03** | Pertumbuhan ukuran berkas lampiran pada media penyimpanan lokal server (`LocalFileStorage`). | Sedang. Berpotensi menghabiskan kapasitas disk server hosting dalam jangka panjang. | Penegakan batas ketat 5 MB per berkas dan maksimal 5 berkas per tiket; merencanakan migrasi ke Object Storage (S3/Azure Blob) untuk fase enterprise berikutnya. | **Ya** | DevOps / IT Infrastructure Lead |

---

## 6. Keputusan yang Dibutuhkan dari Supervisor

1. **Persetujuan Jadwal UAT v1.1.0**:
   - Mengonfirmasi tanggal dan alokasi waktu perwakilan pengguna untuk pelaksanaan User Acceptance Testing versi 1.1.0 pada minggu depan.
2. **Kebijakan Retensi Berkas Lampiran**:
   - Menetapkan durasi retensi penyimpanan berkas lampiran tiket fisik pada server (misal: pengarsipan/penghapusan otomatis berkas untuk tiket yang telah berstatus *Closed* lebih dari 12 bulan).
