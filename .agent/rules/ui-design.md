# UI Design Rules — HelpDeskPro

Berlaku untuk semua pekerjaan di `HelpDeskPro.Web`.

## Prinsip
Ini aplikasi kerja internal yang dipakai 8 jam sehari oleh staf IT dan karyawan.
Prioritas: padat informasi, mudah dipindai, tenang, konsisten. Ini BUKAN landing page.
Referensi rasa: Jira Service Management, Linear, Zendesk agent view.

## Larangan Keras
- Tidak ada gradien, glassmorphism, blur, atau background bermotif.
- Tidak ada emoji di UI, teks, maupun seed data.
- Tidak ada ikon dekoratif. Ikon hanya untuk aksi (edit, hapus, lampiran, salin) dan selalu disertai tooltip.
- Tidak ada animasi selain transisi hover/focus ≤150ms.
- Daftar data WAJIB berupa tabel, bukan grid kartu.
- Tidak ada teks marketing ("Kelola tiket dengan mudah!", "Selamat datang kembali!").
- Tidak ada ilustrasi di empty state.
- Dashboard tidak boleh berupa deretan kartu statistik besar dengan persentase naik/turun.

## Warna (definisikan sebagai token tema, jangan hardcode di komponen)
| Token | Nilai |
|---|---|
| background | #F7F7F5 |
| surface | #FFFFFF |
| border | #E4E4E0 |
| text-primary | #1F1F1F |
| text-secondary | #6B6B6B |
| primary (satu-satunya aksen) | #1F4E79 |
| primary-hover | #173B5C |
| focus-ring | #1F4E79 (outline 2px) |

Warna status hanya untuk badge (teks berwarna + latar sangat pucat, bukan blok solid):
- Prioritas: Critical #B42318, High #C4320A, Medium #B54708, Low #475467
- Status: Open #1F4E79, InProgress #6941C6, Pending #B54708, Resolved #067647, Closed #475467, Cancelled #98A2B3
- SLA lewat: teks merah #B42318 di kolom jatuh tempo, bukan latar baris merah.

## Tipografi
- UI: "IBM Plex Sans". Angka, TicketNo, timestamp: "IBM Plex Mono".
- Ukuran dasar 14px. Judul halaman 20px semibold. Label/caption 12px.
- Maksimal 3 ukuran font dalam satu halaman.
- Sentence case untuk semua label ("Buat tiket", bukan "Buat Tiket").

## Spasi & Bentuk
- Grid 4px.
- Sel tabel: padding 8px 12px, tinggi baris 40px, header tabel sticky.
- Border radius: 4px (tombol, input, badge), 6px (panel, modal). Tidak lebih besar.
- Bayangan hanya untuk dropdown, popover, modal. Panel cukup border 1px.
- Lebar konten maks 1440px. Sidebar navigasi 220px, bisa diciutkan.

## Layout Standar
- Sidebar kiri (navigasi) + top bar tipis (judul halaman, pencarian global, user menu).
- Halaman daftar: baris filter di atas tabel, tombol aksi utama di kanan atas, paging di bawah.
- Halaman detail tiket: kolom kiri (judul, deskripsi, timeline komentar & history), kolom kanan 320px (properti: status, prioritas, assignee, kategori, SLA, requester).

## Komponen
- Gunakan MudBlazor secara konsisten dengan tema kustom sesuai token di atas. Jangan campur library UI lain.
- Tombol: maksimal satu tombol primary per area. Sisanya outlined/text.
- Form: label di atas input, pesan error di bawah input berwarna merah, tombol simpan di kanan bawah.
- Konfirmasi aksi destruktif (batal tiket, hapus lampiran) wajib lewat dialog.

## Copywriting
- Bahasa Indonesia baku tapi lugas, khas aplikasi kantor.
- Empty state: satu kalimat + satu tombol aksi. Contoh: "Belum ada tiket yang di-assign ke Anda."
- Pesan error menjelaskan apa yang harus dilakukan: "Judul wajib diisi, maksimal 200 karakter."
- Waktu: relatif ("2 jam lalu") dengan tooltip waktu lengkap WIB ("9 Nov 2026, 14.32 WIB").

## Data Contoh (Seed)
- Nama orang Indonesia realistis (Rina Kartika, Budi Santoso, Dewi Lestari, Agus Pratama).
- Departemen: IT, Finance, HR, Procurement, Warehouse Cikarang, Sales Jakarta.
- Judul tiket realistis, contoh: "Printer lantai 3 paper jam terus", "Minta akses folder shared drive Procurement", "VPN putus-putus saat WFH", "Laptop baru untuk karyawan onboarding 17 Nov".
- Sertakan kasus tidak ideal: judul panjang, tiket tanpa assignee, tiket SLA lewat, deskripsi kosong.
- DILARANG: "John Doe", "Lorem ipsum", "Test 123", "Acme Corp".

## Interaksi Kecil Wajib
- Shortcut keyboard: `N` tiket baru, `/` fokus ke pencarian, `Esc` tutup dialog.
- Filter terakhir di halaman daftar diingat per user.
- Klik TicketNo menyalin ke clipboard dengan toast singkat "Disalin".
- Detail tiket menampilkan hitung mundur SLA ("Sisa 1j 20m" atau "Lewat 45m").
