# Laporan Implementasi — P-12 UI Blazor: Detail Tiket & Form Buat Tiket

| Item | Isi |
|---|---|
| Modul | UI Blazor — Detail Tiket (`/tickets/{id}`) & Form Buat Tiket (`/tickets/new`) (P-12) |
| Tanggal | 10 November 2026 |
| Target Framework | .NET 8 (Blazor Interactive Server + MudBlazor 7) |
| Acuan Spesifikasi | FSD 4.2 s.d. 4.7 (FR-03 s.d. FR-19), `.agent/rules/ui-design.md` |
| Status | Selesai & Lulus Pengujian (0 Errors, 0 Warnings, 310/310 Backend Tests Passing) |

---

## 1. Ringkasan Implementasi

Pada tahap **P-12 · UI — Detail Tiket & Form Buat Tiket**, kami telah mengimplementasikan modul antarmuka pengguna untuk melihat, mengelola, memperbarui detail tiket, serta membuat tiket baru dengan kepatuhan penuh terhadap panduan desain antarmuka `.agent/rules/ui-design.md` dan aturan fungsional FSD.

---

## 2. Halaman Detail Tiket (`/tickets/{id}`)

Halaman `/tickets/{id}` dirancang menggunakan tata letak dua kolom asimetris: kolom konten utama yang fleksibel di sisi kiri dan panel properti tetap selebar 320px di sisi kanan.

### 2.1 Header Tiket & Tombol Aksi Kontekstual
- **Navigasi Kembali**: Tombol "Daftar tiket" di bagian atas untuk kembali dengan cepat ke daftar tiket.
- **Identitas Tiket**:
  - `TicketNo` menggunakan tipografi `IBM Plex Mono` dalam badge biru muda pucat `#EBF3FB`.
  - Tombol salin nomor tiket di samping badge yang menyalin nomor tiket ke clipboard dan memunculkan toast *"Disalin"* dari `MudSnackbar`.
  - Judul tiket disajikan tebal (`font-size: 20px`, `font-weight: 600`, warna netral gelap `#1E1E1E`).
- **Aksi Kontekstual Berdasarkan Role & Status**:
  - **Ambil tiket**: Ditampilkan untuk staf IT (`Agent`) saat tiket belum memiliki penanggung jawab (`AssigneeId == null`) dan status masih `Open`.
  - **Tugaskan**: Ditampilkan untuk `Supervisor` atau `Admin` untuk membuka dialog penugasan teknisi (`AssignTicketDialog`).
  - **Ubah status**: Ditampilkan untuk staf IT atau pemilik tiket saat tiket masih aktif (belum berstatus terminal `Closed` atau `Cancelled`). Membuka dialog perubahan status (`ChangeStatusDialog`).
  - **Eskalasi**: Ditampilkan untuk eskalasi manual tiket bermasalah ke tingkat yang lebih tinggi (`EscalateDialog`).

### 2.2 Kolom Kiri: Konten, Lampiran, & Linimasa Terpadu
1. **Deskripsi Kendala**:
   - Kartu deskripsi masalah yang ditulis oleh pelapor tiket saat pembuatan tiket.
2. **Daftar Lampiran File**:
   - Menampilkan daftar berkas terlampir berupa chip rapi yang mencantumkan nama file, ukuran dalam format KB/MB yang mudah dibaca, serta tombol unduh langsung.
   - Tombol "Tambah file" yang memungkinkan pengunggahan lampiran tambahan sewaktu-waktu.
3. **Linimasa Terpadu (Unified Timeline)**:
   - Menggabungkan riwayat perubahan status/sistem dan percakapan komentar dalam satu urutan kronologis yang runtut.
   - **Komentar Pelapor/Umum**: Kartu berlatar putih bersih dengan inisial avatar pengguna, nama pengirim, waktu relatif (`RelativeTime`), dan isi komentar.
   - **Komentar Internal**: Khusus untuk catatan internal staf IT, diberi label khusus "Internal" dan latar kuning sangat pucat (`#FEF9C3`) dengan batas tipis `#FEF08A` dan teks `#854D0E`. Komentar ini disaring di API dan hanya terlihat oleh staf IT.
   - **Riwayat Perubahan (History)**: Ditampilkan ringkas berupa satu baris abu-abu netral (`#6B6B6B`) dengan bullet pemisah, misalnya: *Status diubah dari "Open" ke "InProgress" oleh Agus Pratama · 1 jam lalu*.
4. **Kotak Komentar**:
   - Terletak di bagian bawah linimasa.
   - Menyediakan `MudTextField` multilapis dengan placeholder kontekstual.
   - Dilengkapi sakelar toggle *"Komentar internal"* yang hanya dapat diakses oleh staf IT (`Agent`, `Supervisor`, `Admin`).
   - Tombol "Kirim tanggapan" primer di sisi kanan bawah.

### 2.3 Kolom Kanan: Panel Properti Tiket (Fixed 320px)
Panel ringkasan metadata tetap berukuran 320px (`width: 320px; flex: 0 0 320px`) yang memuat:
- **Status**: Komponen atomik `<StatusBadge Status="@_ticket.Status" />`.
- **Prioritas**: Komponen atomik `<PriorityBadge Priority="@_ticket.Priority" />`.
- **Kategori**: Nama kategori permasalahan tiket.
- **Assignee**: Nama teknisi penanggung jawab, atau teks abu-abu miring *"Belum di-assign"*.
- **Requester**: Nama lengkap pembuat tiket.
- **Dibuat**: Komponen `<RelativeTime>` dengan tooltip zona waktu WIB resmi.
- **Target SLA Respons**: Status pencapaian atau waktu target respons pertama.
- **Target SLA Resolusi**: Indikator hitung mundur `<SlaCountdown>` dengan tipografi `IBM Plex Mono` (warna merah peringatan `#B42318` jika SLA breached/terlampaui).
- **Level Eskalasi**: Badge level eskalasi aktif tiket.

### 2.4 Penanganan Konflik Konkurensi (409 Conflict)
- Jika pengguna lain mengubah tiket secara bersamaan saat dialog atau form disubmit, sistem menangkap status HTTP 409.
- Memunculkan alert banner peringatan berwarna merah/kuning: *"Tiket sudah diubah pengguna lain, muat ulang halaman."*
- Disertai tombol "Muat ulang" yang langsung mengambil ulang data tiket terbaru dari server tanpa perlu me-reload keseluruhan halaman browser.

---

## 3. Form Buat Tiket Baru (`/tickets/new`)

Halaman pembuatan tiket dapat diakses melalui tombol "Buat tiket" pada daftar tiket maupun melalui shortcut keyboard global huruf `N`.

### 3.1 Field-Field Form & Validasi
1. **Judul Tiket (`MudTextField`)**:
   - Wajib diisi, panjang 5 hingga 200 karakter.
   - Dilengkapi *character counter* di kanan bawah field.
2. **Kategori Tiket (`MudSelect`)**:
   - Dropdown pilihan kategori aktif yang diambil secara dinamis dari API master data.
   - Wajib dipilih.
3. **Prioritas Tiket (`MudSelect`)**:
   - Pilihan: `Low`, `Medium`, `High`, `Critical`.
   - Default: `Medium`.
   - **Teks Kecil Target SLA Dinamis**: Di bawah dropdown prioritas, secara reaktif menampilkan estimasi target SLA sesuai prioritas yang dipilih (misal: *Target SLA: Respons 4 jam, Resolusi 24 jam*).
4. **Deskripsi Kendala (`MudTextField`)**:
   - Field catatan rinci permasalahan (multiline, opsional, maksimal 4000 karakter).
5. **Zona Unggah Lampiran (Drag & Drop Zone)**:
   - Area drag-and-drop khusus (`.file-dropzone`) dengan ikon unggah dan instruksi jelas.
   - **Validasi Sisi Klien Ketat**:
     - Maksimal 5 berkas per tiket.
     - Ukuran berkas maksimal 5 MB per berkas.
     - Ekstensi yang diizinkan: `.jpg`, `.jpeg`, `.png`, `.pdf`, `.docx`, `.xlsx`, `.txt`, `.log`.
   - Daftar berkas yang dipilih ditampilkan langsung dengan ukuran dan tombol hapus individual.

### 3.2 Feedback & Alur Setelah Sukses
- Menekan tombol "Buat tiket" mengirim data ke `POST /api/tickets` dan mengunggah lampiran terpilih.
- Setelah sukses: sistem langsung menavigasi ke halaman detail tiket yang baru dibuat (`/tickets/{newTicketId}`) disertai toast feedback:
  *"Tiket HD-2026-xxxxxx dibuat"*.

---

## 4. Dialog Modal Reusable

1. **`ChangeStatusDialog.razor`**:
   - Mengatur transisi status yang valid sesuai FSD.
   - Mengharuskan pengisian alasan jika mengubah ke status `Pending` atau `Cancelled`.
   - Mengharuskan pengisian catatan teknis jika mengubah ke status `Resolved`.
   - Memvalidasi pembukaan kembali tiket (`Reopened`) maksimal 7 hari setelah berstatus resolved.
2. **`AssignTicketDialog.razor`**:
   - Menampilkan daftar staf IT aktif untuk ditugaskan ke tiket.
3. **`EscalateDialog.razor`**:
   - Menyediakan form catatan eskalasi manual (minimal 10 karakter) untuk eskalasi ke supervisor.

---

## 5. Kepatuhan Desain Antarmuka (`.agent/rules/ui-design.md`)

| Kriteria | Evaluasi Kepatuhan |
|---|---|
| **Bebas Gradien** | Seluruh elemen (kartu, header, timeline, badge) menggunakan warna solid murni. |
| **Bebas Emoji** | Tidak ada emoji apa pun di seluruh antarmuka dan pesan toast/dialog. |
| **Bebas Ikon Dekoratif** | Ikon hanya digunakan untuk fungsi bermakna (lampiran, salin, aksi tombol). |
| **Tipografi Resmi** | Teks utama menggunakan `IBM Plex Sans`, nomor tiket dan waktu menggunakan `IBM Plex Mono`. |
| **Sentence Case** | Seluruh teks tombol, label form, dan pesan dialog menggunakan sentence case standar bahasa Indonesia. |
| **Panel Samping 320px** | Panel properti sisi kanan menggunakan lebar tetap 320px tanpa distorsi pada layar desktop. |
| **Tampilan Timeline** | Komentar internal menggunakan latar kuning sangat pucat (`#FEF9C3`) dan riwayat sistem menggunakan satu baris abu-abu ringkas. |

---

## 6. Verifikasi & Pengujian

- **Kompilasi Solusi**: `dotnet build HelpDeskPro.sln` menghasilkan **0 Errors, 0 Warnings** (dengan `TreatWarningsAsErrors=true`).
- **Pengujian Unit & Integrasi**:
  - `HelpDeskPro.UnitTests`: **275 passed**, 0 failed.
  - `HelpDeskPro.IntegrationTests`: **35 passed**, 0 failed.
  - Total pengujian: **310 passed, 0 failed**.
- **Pengujian Visual**: Tangkapan layar headless Chrome beresolusi 1366x850 pada `/tickets/1` dan `/tickets/new` memvalidasi estetika tata letak, proporsi kolom, penataan timeline, dan responsivitas komponen form.

