# ADR 002: Perhitungan SLA Berdasarkan Jam Kerja dan Kebijakan Tidak Menghitung Ulang Tiket Eksisting

## Status
Diterima (Accepted)

## Tanggal
10 November 2026

## Konteks & Latar Belakang
Pada implementasi rilis v1.0.0, batas waktu SLA dihitung berdasarkan jam kalender murni secara linear (24 jam sehari, 7 hari seminggu). Hal ini menimbulkan masalah operasional bagi tim helpdesk/teknisi: tiket yang dilaporkan pada Jumat sore atau malam hari langsung mengalami *breach* saat akhir pekan ketika tim pendukung sedang tidak bertugas.

Melalui Change Request 002 (CR-002), organisasi memutuskan agar perhitungan SLA diubah menjadi berbasis **Jam Kerja** (*Business Hours*):
- Jam kerja operasional: Senin sampai Jumat, pukul 08:00 – 17:00 WIB (9 jam kerja per hari).
- Akhir pekan (Sabtu & Minggu) dan Hari Libur Nasional tidak dihitung sebagai jam kerja.
- Tiket yang dibuat di luar jam kerja mulai dihitung pada pukul 08:00 WIB hari kerja berikutnya.
- Durasi status *Pending* hanya menghitung menit jam kerja nyata yang terlewati.

Pertanyaan arsitektural penting: **Apakah tiket-tiket yang sudah dibuat sebelum implementasi CR-002 harus dihitung ulang tanggal jatuh temponya?**

## Keputusan
Diputuskan bahwa **TIKET YANG SUDAH ADA (EKSISTING) TIDAK AKAN DIHITUNG ULANG**.
Perhitungan jam kerja hanya berlaku untuk:
1. Tiket baru yang dibuat sejak rilis CR-002.
2. Penyesuaian durasi saat tiket aktif kembali dari status *Pending* ke *InProgress* sejak rilis CR-002.

### Alasan Keputusan:
1. **Integritas Audit & Historis**:
   Menghitung ulang tanggal jatuh tempo (`ResponseDueAt` dan `ResolutionDueAt`) pada tiket-tiket masa lalu akan merusak integritas pelaporan historis dan audit trail yang telah disepakati antara manajemen dan pemohon layanan.
2. **Pencegahan Efek Samping Pelanggaran SLA Palsu**:
   Tiket yang sebelumnya berstatus patuh dapat tiba-tiba berubah status pelanggaran jika tanggal historis dimanipulasi secara retroaktif.
3. **Immutability Nilai Kontrak Layanan**:
   Batas waktu SLA pada tiket berfungsi sebagai komitmen waktu layanan yang diterbitkan saat tiket dibuat. Nilai kontrak tersebut tidak boleh diubah secara sepihak di kemudian hari.
4. **Stabilitas Test Suite**:
   Menjaga seluruh 354 test suite yang telah lulus 100% tetap hijau tanpa menimbulkan regresi pada data fixture lama.

## Konsekuensi
- **Positif**:
  - Pelaporan historis dan data audit tetap konsisten dan terpercaya.
  - Tidak diperlukan proses batch migrasi data yang berisiko pada tabel `Tickets`.
  - Sistem bersih dan modular melalui abstraksi `IBusinessCalendar` dan `ISlaCalculator`.
- **Mitigasi**:
  - Kolom `Holidays` dikelola secara dinamis melalui antarmuka Admin agar kalender hari libur tahun berjalan selalu mutakhir.
