# Laporan Implementasi — P-10 UI Blazor: Shell & Tema

| Item | Isi |
|---|---|
| Modul | UI Blazor — Shell, Layout Navigasi Berbasis Peran & Sistem Tema (P-10) |
| Tanggal | 10 November 2026 |
| Target Framework | .NET 8 (Blazor Interactive Server / Static SSR + MudBlazor 7) |
| Status | Selesai & Lulus Verifikasi (0 Errors, 0 Warnings, 310/310 Backend Tests Passing) |

---

## 1. Ringkasan Implementasi

Pada tahap **P-10 · UI Blazor — Shell & Tema**, kami telah membangun fondasi antarmuka pengguna HelpDeskPro berbasis MudBlazor dengan kepatuhan penuh terhadap panduan desain internal `.agent/rules/ui-design.md` dan standar pengkodean `.agent/rules/coding-standard.md`. Tidak ada halaman fitur operasional yang dibuat pada tahap ini; fokus diarahkan secara ketat pada kerangka antarmuka, tata letak shell responsif, penegakan tema visual utilitarian, integrasi otentikasi JWT client-side, komponen atomik reusable, dan shortcut keyboard global.

### 1.1 Tema MudBlazor Kustom (`Theme/AppTheme.cs`)
Seluruh token warna, tipografi, dan parameter tata letak dikonsolidasikan dalam satu file tema tunggal (`AppTheme.cs`):
- **Palet Warna Netral & Fungsional**:
  - `Background`: `#F7F7F5` (abu-abu sangat terang, hangat, ramah di mata)
  - `Surface`: `#FFFFFF` (putih murni untuk kartu, tabel, drawer, dan dialog)
  - `Border / Divider`: `#E4E4E0` (garis pemisah netral 1px)
  - `TextPrimary`: `#1F1F1F` (kontras tinggi untuk teks utama)
  - `TextSecondary`: `#6B6B6B` (abu-abu netral untuk metadata, caption, dan placeholder)
  - `Primary`: `#1F4E79` (Deep Blue utilitarian)
  - `PrimaryHover / Darken`: `#173B5C`
  - `Warning`: `#B54708`
  - `Error`: `#B42318`
  - `Success`: `#027A48`
  - `Info`: `#026AA2`
- **Sistem Elevasi & Border**:
  - `Elevation = 0` di seluruh antarmuka. Kedalaman visual dibangun menggunakan batas fisik garis 1px solid `#E4E4E0` dan kontras bidang warna, bukan drop-shadow lembut (larangan tegas rules UI dipatuhi).
  - Border radius: `4px` untuk tombol, input, dan chip status; `6px` untuk modal card.
- **Tipografi Resmi**:
  - Teks antarmuka, label, tombol, dan judul: **IBM Plex Sans** (ukuran H6 20px semi-bold, Body1 14px regular, Body2 13px regular, Caption 12px).
  - Data numerik, kode tiket, timestamp, dan SLA countdown: **IBM Plex Mono** (angka tabular monospaced untuk perataan vertikal optimal).
  - Tidak ada kapitalisasi huruf besar semua (*NO UPPERCASE* pada tombol MudBlazor; menggunakan `TextTransform = "none"` dan format sentence case).

---

### 1.2 Shell Layout Terpadu (`Components/Layout/MainLayout.razor`)
Layout aplikasi menyajikan kerangka kerja operasional yang bersih dan padat informasi:
1. **Sidebar Navigasi (220px, Collapsible)**:
   - Lebar tetap 220px, dapat diciutkan/dibuka dengan tombol hamburger di top bar.
   - Brand logo teks "HelpDeskPro" tipis dan profesional tanpa logo berlebihan.
   - Menu dinamis terisolasi sesuai peran pengguna yang sedang login:
     - **Requester**: Tiket Saya
     - **Agent**: Tiket Saya, Semua Tiket
     - **Supervisor**: Tiket Saya, Semua Tiket, Dashboard, Eskalasi
     - **Admin**: Tiket Saya, Semua Tiket, Dashboard, Eskalasi, Master Data
   - Tanda versi sistem (`v1.0.0-rc`) di footer sidebar dengan font mono.
2. **Top Bar Tipis (50px)**:
   - Latar belakang putih dengan batas bawah 1px `#E4E4E0`.
   - Judul halaman dinamis di sisi kiri.
   - **Global Search Box**: ID `#global-search`, lebar 320px dengan placeholder `Cari tiket... [/]`, transisi ringkas saat fokus.
   - **Lonceng Notifikasi**: Ikon lonceng dengan MudBadge unread count otomatis.
   - **User Profile Menu**: Menampilkan nama lengkap, peran (badge kecil), departemen, dan menu dropdown dengan opsi profil dan Logout.

---

### 1.3 Halaman Login Sederhana (`Components/Pages/Login.razor`)
Mengikuti larangan tegas rules UI (tanpa ilustrasi grafis kartun, tanpa gradien latar belakang, tanpa ornamen berlebihan):
- Kartu form berada di tengah layar (*centered card*, max-width 380px).
- Latar permukaan putih murni (`#FFFFFF`), border 1px `#E4E4E0`, border-radius 6px.
- Header form: Teks brand `HelpDeskPro` biru tua 22px dan sub-judul `Masuk ke akun Anda` 14px abu-abu.
- Field input standar: NIK / Email dan Kata Sandi dengan penanganan *enter to submit*.
- Checkbox "Ingat saya" dan tombol aksi primer "Masuk" dengan animasi spinner CSS ringkas saat pemrosesan.
- Penanganan umpan balik kesalahan autentikasi yang jelas menggunakan container alert merah pucat.

---

### 1.4 Typed HttpClient & State Autentikasi JWT
- **`CustomAuthenticationStateProvider`**:
  - Mengambil token JWT dari `ILocalStorageService`.
  - Melakukan decode payload JWT (klaim name, email, role, department, sub) tanpa membebani browser dengan library eksternal.
  - Memperbarui `AuthenticationState` secara reaktif saat login dan logout.
- **`AuthHeaderHandler` (`DelegatingHandler`)**:
  - Menginjeksi header `Authorization: Bearer <jwt-token>` ke setiap panggilan API backend.
  - Mendeteksi respons `401 Unauthorized`: otomatis menghapus token lokal dan melakukan navigasi paksa ke halaman `/login`.
- **`IApiClient` & `ApiClient`**:
  - Layanan typed HttpClient untuk operasi autentikasi (`LoginAsync`, `GetCurrentUserAsync`) dan notifikasi (`GetUnreadNotificationCountAsync`).
- **`LocalStorageService`**:
  - Abstraksi aman berbasis `IJSRuntime` untuk persistensi sesi di browser `localStorage`.

---

### 1.5 Komponen Atomik Reusable (`Components/Common/`)
Setiap komponen dibangun dengan prinsip modularitas tinggi dan kepatuhan token desain:
1. **`StatusBadge.razor`**:
   - Menampilkan status tiket (`Open`, `InProgress`, `Pending`, `Resolved`, `Closed`, `Cancelled`).
   - Warna teks kontras tinggi dengan warna latar belakang pucat (opacity ~12%), border radius 4px, font 12px medium.
2. **`PriorityBadge.razor`**:
   - Menampilkan prioritas (`Critical`, `High`, `Medium`, `Low`).
   - Dilengkapi titik indikator berwarna (*colored dot* 6px) di sebelah teks label sesuai aturan FSD (Critical: `#B42318`, High: `#C4320A`, Medium: `#B54708`, Low: `#475467`).
3. **`SlaCountdown.razor`**:
   - Menggunakan tipografi `IBM Plex Mono` untuk angka waktu.
   - Format human-readable: "Sisa 1j 20m" jika masih dalam batas waktu, atau teks merah peringatan "Lewat 45m" jika sudah terlewati.
4. **`RelativeTime.razor`**:
   - Menggunakan font `IBM Plex Mono`.
   - Menampilkan waktu relatif ringkas (misal: "2 jam lalu", "3 hari lalu") dilengkapi tooltip MudTooltip yang memaparkan timestamp absolut zona WIB resmi ("10 Nov 2026, 14.32 WIB").
5. **`EmptyState.razor`**:
   - Menolak ilustrasi kosong/kartun.
   - Menyajikan satu kalimat informatif yang jelas dan satu tombol tindakan utama (misal: "Belum ada tiket yang dibuat" + tombol "Buat Tiket Baru").
6. **`ConfirmDialog.razor`**:
   - Modal konfirmasi berbasis `MudDialogInstance` untuk tindakan destruktif atau krusial (misal: eskalasi, penutupan tiket, pembatalan).

---

### 1.6 Shortcut Keyboard Global (`wwwroot/js/shortcuts.js`)
Pintasan papan ketik dipasang di level window listener:
- **`N`**: Navigasi instan ke halaman pembuatan tiket baru (`/tickets/new`) saat pengguna tidak sedang berada di dalam elemen teks input.
- **`/`**: Memfokuskan kursor secara instan ke kolom pencarian tiket `#global-search`.
- **`Esc`**: Menghilangkan fokus dari input pencarian atau menutup modal dialog aktif.

---

## 2. Struktur Berkas yang Dibuat & Diperbarui

```
src/HelpDeskPro.Web/
├── Components/
│   ├── Common/
│   │   ├── ConfirmDialog.razor
│   │   ├── EmptyState.razor
│   │   ├── PriorityBadge.razor
│   │   ├── RedirectToLogin.razor
│   │   ├── RelativeTime.razor
│   │   ├── SlaCountdown.razor
│   │   └── StatusBadge.razor
│   ├── Layout/
│   │   └── MainLayout.razor
│   ├── Pages/
│   │   ├── Home.razor
│   │   └── Login.razor
│   ├── App.razor
│   ├── Routes.razor
│   └── _Imports.razor
├── Services/
│   ├── ApiClient.cs
│   ├── AuthHeaderHandler.cs
│   ├── CustomAuthenticationStateProvider.cs
│   ├── IApiClient.cs
│   └── ILocalStorageService.cs
├── Theme/
│   └── AppTheme.cs
├── wwwroot/
│   ├── js/
│   │   └── shortcuts.js
│   ├── app.css
│   └── favicon.png
├── appsettings.json
├── Program.cs
└── HelpDeskPro.Web.csproj
```

---

## 3. Hasil Pengujian & Verifikasi

### 3.1 Kompilasi & Build
- Konfigurasi: `dotnet build HelpDeskPro.sln`
- Hasil: **0 Error(s), 0 Warning(s)** dengan parameter `TreatWarningsAsErrors=true`.

### 3.2 Regresi Test Suite
- Konfigurasi: `dotnet test HelpDeskPro.sln`
- Unit Tests: **275 Passed, 0 Failed, 0 Skipped** (net8.0)
- Integration Tests: **35 Passed, 0 Failed, 0 Skipped** (net8.0)
- Total Pengujian: **310 Passed** (100% lulus tanpa regresi).

### 3.3 Verifikasi Visual Headless Browser
Aplikasi frontend `HelpDeskPro.Web` dijalankan pada `http://localhost:5015` dan diverifikasi menggunakan headless browser untuk memastikan tata letak dan estetika sesuai aturan:
1. **Halaman Login (`/login`)**:
   - Card putih di tengah, border 1px netral, tanpa grafis/animasi mengganggu.
   - Screenshot tersimpan dan diverifikasi: `login-page.png` (21.7 KB).
2. **Shell Antarmuka Utama & Komponen Reusable (`/`)**:
   - Sidebar 220px rapi, top bar 50px dengan search bar monospaced shortcut `[/]`.
   - Demo showcase seluruh komponen reusable (StatusBadge, PriorityBadge, SlaCountdown, RelativeTime, EmptyState).
   - Screenshot tersimpan dan diverifikasi: `shell-overview.png` (60.1 KB).

---

## 4. Kesimpulan & Langkah Selanjutnya

Tahap **P-10 · UI Blazor: Shell & Tema** telah selesai secara menyeluruh. Kerangka antarmuka telah siap untuk diintegrasikan dengan fitur operasional tiket. Seluruh prasyarat arsitektur dan aturan desain telah terpenuhi.

Sistem siap melanjutkan ke tahap berikutnya:
👉 **P-11 · UI — Daftar Tiket (Filter, Sort, Paging, Matriks Akses)**.

