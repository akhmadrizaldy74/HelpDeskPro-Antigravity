# Laporan Fase P-16: CR-002 Perhitungan SLA Berdasarkan Jam Kerja (Business Hours SLA)

**Tanggal:** 10 November 2026  
**Status:** Selesai (100% Lulus)  
**Tautan Terkait:**  
- [Change Request 002](file:///c:/Users/Akhmad%20Rizaldy/.gemini/antigravity-ide/scratch/HelpDeskPro-Antigravity/HelpDeskPro-Antigravity/docs/CR/CR-002.md)  
- [ADR 002: Business Hours SLA](file:///c:/Users/Akhmad%20Rizaldy/.gemini/antigravity-ide/scratch/HelpDeskPro-Antigravity/HelpDeskPro-Antigravity/docs/ADR/002-business-hours-sla.md)  
- [Data Dictionary](file:///c:/Users/Akhmad%20Rizaldy/.gemini/antigravity-ide/scratch/HelpDeskPro-Antigravity/HelpDeskPro-Antigravity/docs/database/data-dictionary.md)  

---

## 1. Ringkasan Implementasi

Kebutuhan **CR-002 (Perhitungan SLA Berdasarkan Jam Kerja)** telah selesai diimplementasikan secara komprehensif dengan pendekatan arsitektur bersih terpusat:

1. **Aturan Jam Kerja & Zona Waktu**:
   - Jam kerja resmi: **Senin – Jumat, pukul 08.00 – 17.00 WIB** (Asia/Jakarta, UTC+7).
   - Setiap hari kerja memiliki 9 jam kerja aktif = 540 menit kerja.
   - Hari Sabtu, Minggu, dan hari libur nasional memiliki 0 jam kerja aktif.
   - Tiket yang dibuat di luar jam kerja (sebelum 08.00, setelah 17.00, atau pada akhir pekan/hari libur) secara otomatis dihitung mulai pukul 08.00 WIB pada hari kerja berikutnya.

2. **Pengelolaan Hari Libur Nasional (`Holidays`)**:
   - Entitas baru `Holiday` (`Id`, `Date`, `Name`) di layer Domain.
   - Migrasi Entity Framework Core baru `20261110164950_AddHolidaysTable.cs` dengan indeks unik pada kolom `Date`.
   - Modul CQRS lengkap di layer Application (`GetHolidaysQuery`, `CreateHolidayCommand`, `UpdateHolidayCommand`, `DeleteHolidayCommand`) dilengkapi validasi duplikasi tanggal dan batas karakter.
   - API Controller `MasterHolidaysController` (`/api/master/holidays`) dengan otorisasi khusus `RequireAdmin`.
   - Halaman admin Blazor `Holidays.razor` (`/admin/holidays`) dengan filter tahun, dialog penambahan/pengubahan `HolidayFormDialog.razor`, konfirmasi penghapusan, dan integrasi tautan pada sidebar `MainLayout.razor`.

3. **Sentralisasi Logika Kalender & Kalkulasi SLA**:
   - Diekstrak ke antarmuka independen `IBusinessCalendar` dan `ISlaCalculator` tanpa menyebarkan logika perhitungan tanggal di berbagai handler.
   - Implementasi `BusinessCalendar`: Konversi akurat UTC $\leftrightarrow$ WIB, pemeriksaan hari kerja (`IsWorkingDay`), penambahan menit kerja (`AddWorkingMinutes`), dan kalkulasi durasi kerja antara dua waktu (`GetWorkingMinutesBetween`).
   - Implementasi `BusinessHoursSlaCalculator`: Menghitung jatuh tempo respons dan resolusi saat tiket dibuat (`CalculateDueDatesAsync`), penyesuaian jatuh tempo saat status `Pending` dilanjutkan kembali (`AdjustDueDatesForPausedDurationAsync`), serta kalkulasi sisa waktu (`CalculateRemainingMinutes`).

4. **Kebijakan Tiket yang Sudah Ada (Non-Recalculation)**:
   - Sesuai instruksi dan keputusan arsitektur pada `docs/ADR/002-business-hours-sla.md`, tiket yang sudah ada **tidak dihitung ulang**. Kolom `ResponseDueAt` dan `ResolutionDueAt` pada tiket yang sudah tersimpan tetap dipertahankan.

5. **Penyesuaian Transisi Status Tiket**:
   - Pada saat tiket keluar dari status `Pending` menuju status aktif lainnya (`InProgress`, dll.), waktu jeda hanya menghitung jam kerja aktif yang terlewati (misal: tiket jeda Senin 16.30 WIB dan dilanjutkan Selasa 09.30 WIB hanya dihitung terjeda 120 menit kerja, waktu malam hari tidak memotong SLA).

---

## 2. Hasil Pengujian & Verifikasi Kualitas

| Metrik | Sebelum CR-002 | Setelah CR-002 | Keterangan |
|---|---|---|---|
| **Total Test Suite** | 354 tests | **376 tests** | **+22 test baru** (100% lulus, 0 gagal, 0 di-skip) |
| **Unit Tests** | 318 tests | **339 tests** | **+21 test baru** |
| **Integration Tests** | 36 tests | **37 tests** | **+1 test baru** (`MasterHolidays_Crud_ReturnsSuccess`) |
| **Coverage Application** | 86,6% | **83,5%** | Target $\ge 70\%$ terlampaui |
| **Coverage Domain** | 93,3% | **90,9%** | Sangat baik |
| **Build Status** | 0 Warning, 0 Error | **0 Warning, 0 Error** | Release build bersih (`TreatWarningsAsErrors=true`) |

---

## 3. Rincian Skenario Uji Wajib (6 Skenario Inti)

Seluruh 6 skenario wajib yang diminta telah diuji secara mendalam pada `BusinessHoursSlaCalculatorTests.cs`:

1. **Dibuat di jam kerja**: Selasa 10.00 WIB + 120 menit resolusi $\rightarrow$ jatuh tempo Selasa 12.00 WIB (`Scenario1_CreatedDuringWorkHours_ShouldAdvanceWithinSameDay`).
2. **Sebelum jam 08.00 WIB**: Rabu 06.30 WIB + 60 menit resolusi $\rightarrow$ mulai dihitung dari 08.00 WIB $\rightarrow$ jatuh tempo Rabu 09.00 WIB (`Scenario2_CreatedBeforeWorkHours_ShouldStartAt0800Wib`).
3. **Setelah jam 17.00 WIB**: Kamis 19.00 WIB + 120 menit resolusi $\rightarrow$ mulai dihitung Jumat 08.00 WIB $\rightarrow$ jatuh tempo Jumat 10.00 WIB (`Scenario3_CreatedAfter1700Wib_ShouldStartNextWorkingDayAt0800Wib`).
4. **Jumat sore melewati akhir pekan**: Sesuai contoh pada prompt: Tiket Medium (24 jam kerja = 1440 menit) dibuat Jumat 16.00 WIB $\rightarrow$ Jumat 1 jam + Sabtu-Minggu libur + Senin 9 jam + Selasa 9 jam + Rabu 5 jam $\rightarrow$ jatuh tempo **Rabu 13.00 WIB** (`Scenario4_CreatedFridayAfternoon_ShouldRollOverWeekend`).
5. **Melewati hari libur nasional**: Tiket Medium dibuat Jumat 16.00 WIB dan Senin 16 Nov adalah hari libur nasional $\rightarrow$ jatuh tempo bergeser ke **Kamis 13.00 WIB** (`Scenario5_CreatedBeforeHoliday_ShouldRollOverNationalHoliday`).
6. **Pending yang melewati malam hari**: Tiket dijeda Senin 16.30 WIB dan dilanjutkan Selasa 09.30 WIB $\rightarrow$ hanya terhitung 120 menit jeda kerja (30 menit Senin + 90 menit Selasa, malam hari 0 menit), target SLA resolusi diperpanjang tepat 120 menit kerja (`Scenario6_PendingStatusOvernight_ShouldOnlyCountWorkingMinutesInPause`).

---

## 4. Rincian Uji CRUD Hari Libur
Diuji pada `HolidayTests.cs` (Unit) dan `MasterControllersIntegrationTests.cs` (Integration):
- `GetHolidays_ReturnsAllHolidaysOrderedByDate`
- `GetHolidays_WithYearFilter_ReturnsOnlyHolidaysInYear`
- `CreateHoliday_WhenValid_AddsHoliday`
- `CreateHoliday_WhenDateExists_ThrowsBusinessRuleException`
- `CreateHolidayValidator_WhenValid_PassesValidation`
- `CreateHolidayValidator_WhenNameEmpty_FailsValidation`
- `UpdateHoliday_WhenValid_UpdatesHoliday`
- `UpdateHoliday_WhenNotFound_ThrowsNotFoundException`
- `UpdateHoliday_WhenDateConflictWithOtherHoliday_ThrowsBusinessRuleException`
- `UpdateHolidayValidator_WhenValid_PassesValidation`
- `UpdateHolidayValidator_WhenIdZero_FailsValidation`
- `DeleteHoliday_WhenExists_RemovesHoliday`
- `DeleteHoliday_WhenNotFound_ThrowsNotFoundException`
- `MasterHolidays_Crud_ReturnsSuccess` (Integration: GET, POST, PUT, DELETE)
- `MasterEndpoints_WhenCalledByAgent_Returns403Forbidden` (Role security check)
