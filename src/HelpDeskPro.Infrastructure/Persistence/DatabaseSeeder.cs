namespace HelpDeskPro.Infrastructure.Persistence;

using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Departments.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database sudah di-seed sebelumnya.");
            return;
        }

        _logger.LogInformation("Memulai seeding data HelpDeskPro...");

        // 1. Departments
        var itDept = new Department { Code = "IT", Name = "IT Department", IsActive = true };
        var finDept = new Department { Code = "FIN", Name = "Finance", IsActive = true };
        var hrDept = new Department { Code = "HR", Name = "Human Resources", IsActive = true };
        var procDept = new Department { Code = "PROC", Name = "Procurement", IsActive = true };
        var whDept = new Department { Code = "WH-CKR", Name = "Warehouse Cikarang", IsActive = true };
        var salesDept = new Department { Code = "SLS-JKT", Name = "Sales Jakarta", IsActive = true };

        _context.Departments.AddRange(itDept, finDept, hrDept, procDept, whDept, salesDept);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Categories
        var catHardware = new Category { Name = "Hardware", IsActive = true };
        var catSoftware = new Category { Name = "Software", IsActive = true };
        var catNetwork = new Category { Name = "Jaringan", IsActive = true };
        var catAccess = new Category { Name = "Akses & Akun", IsActive = true };
        var catEmail = new Category { Name = "Email", IsActive = true };
        var catPrinter = new Category { Name = "Printer", IsActive = true };

        _context.Categories.AddRange(catHardware, catSoftware, catNetwork, catAccess, catEmail, catPrinter);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. SlaPolicies
        var slaCritical = new SlaPolicy { Priority = TicketPriority.Critical, ResponseTimeMinutes = 30, ResolutionTimeMinutes = 240 };
        var slaHigh = new SlaPolicy { Priority = TicketPriority.High, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 480 };
        var slaMedium = new SlaPolicy { Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440 };
        var slaLow = new SlaPolicy { Priority = TicketPriority.Low, ResponseTimeMinutes = 480, ResolutionTimeMinutes = 4320 };

        _context.SlaPolicies.AddRange(slaCritical, slaHigh, slaMedium, slaLow);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Roles
        string[] roles = ["Admin", "Supervisor", "Agent", "Requester"];
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = role, NormalizedName = role.ToUpperInvariant() });
            }
        }

        // 5. Users
        var devPassword = _configuration["SeedSettings:DefaultPassword"] ?? "Dev@Passw0rd123!";

        // Lead Supervisor
        var sup1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "supervisor.it@helpdeskpro.local",
            Email = "supervisor.it@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Dewi Lestari",
            EmployeeNo = "EMP002",
            DepartmentId = itDept.Id
        };
        await _userManager.CreateAsync(sup1, devPassword);
        await _userManager.AddToRoleAsync(sup1, "Supervisor");

        // Operational Supervisor (reports to Dewi Lestari)
        var sup2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "supervisor.ops@helpdeskpro.local",
            Email = "supervisor.ops@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Hendra Wijaya",
            EmployeeNo = "EMP003",
            DepartmentId = itDept.Id,
            SupervisorId = sup1.Id
        };
        await _userManager.CreateAsync(sup2, devPassword);
        await _userManager.AddToRoleAsync(sup2, "Supervisor");

        // Admin
        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "admin@helpdeskpro.local",
            Email = "admin@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Budi Santoso",
            EmployeeNo = "EMP001",
            DepartmentId = itDept.Id,
            SupervisorId = sup1.Id
        };
        await _userManager.CreateAsync(admin, devPassword);
        await _userManager.AddToRoleAsync(admin, "Admin");

        // 4 IT Agents (reporting to Hendra Wijaya)
        var agentAgus = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent.agus@helpdeskpro.local",
            Email = "agent.agus@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Agus Pratama",
            EmployeeNo = "EMP004",
            DepartmentId = itDept.Id,
            SupervisorId = sup2.Id
        };
        await _userManager.CreateAsync(agentAgus, devPassword);
        await _userManager.AddToRoleAsync(agentAgus, "Agent");

        var agentRina = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent.rina@helpdeskpro.local",
            Email = "agent.rina@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Rina Kartika",
            EmployeeNo = "EMP005",
            DepartmentId = itDept.Id,
            SupervisorId = sup2.Id
        };
        await _userManager.CreateAsync(agentRina, devPassword);
        await _userManager.AddToRoleAsync(agentRina, "Agent");

        var agentFajar = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent.fajar@helpdeskpro.local",
            Email = "agent.fajar@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Fajar Nugraha",
            EmployeeNo = "EMP006",
            DepartmentId = itDept.Id,
            SupervisorId = sup2.Id
        };
        await _userManager.CreateAsync(agentFajar, devPassword);
        await _userManager.AddToRoleAsync(agentFajar, "Agent");

        var agentSiti = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent.siti@helpdeskpro.local",
            Email = "agent.siti@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Siti Rahma",
            EmployeeNo = "EMP007",
            DepartmentId = itDept.Id,
            SupervisorId = sup2.Id
        };
        await _userManager.CreateAsync(agentSiti, devPassword);
        await _userManager.AddToRoleAsync(agentSiti, "Agent");

        // 5 Requesters from other departments
        var reqBambang = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "requester.bambang@helpdeskpro.local",
            Email = "requester.bambang@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Bambang Suryono",
            EmployeeNo = "EMP008",
            DepartmentId = finDept.Id
        };
        await _userManager.CreateAsync(reqBambang, devPassword);
        await _userManager.AddToRoleAsync(reqBambang, "Requester");

        var reqNurul = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "requester.nurul@helpdeskpro.local",
            Email = "requester.nurul@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Nurul Hidayah",
            EmployeeNo = "EMP009",
            DepartmentId = hrDept.Id
        };
        await _userManager.CreateAsync(reqNurul, devPassword);
        await _userManager.AddToRoleAsync(reqNurul, "Requester");

        var reqEko = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "requester.eko@helpdeskpro.local",
            Email = "requester.eko@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Eko Prasetyo",
            EmployeeNo = "EMP010",
            DepartmentId = procDept.Id
        };
        await _userManager.CreateAsync(reqEko, devPassword);
        await _userManager.AddToRoleAsync(reqEko, "Requester");

        var reqTri = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "requester.tri@helpdeskpro.local",
            Email = "requester.tri@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Tri Wahyuni",
            EmployeeNo = "EMP011",
            DepartmentId = whDept.Id
        };
        await _userManager.CreateAsync(reqTri, devPassword);
        await _userManager.AddToRoleAsync(reqTri, "Requester");

        var reqDian = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "requester.dian@helpdeskpro.local",
            Email = "requester.dian@helpdeskpro.local",
            EmailConfirmed = true,
            FullName = "Dian Sastrowardoyo",
            EmployeeNo = "EMP012",
            DepartmentId = salesDept.Id
        };
        await _userManager.CreateAsync(reqDian, devPassword);
        await _userManager.AddToRoleAsync(reqDian, "Requester");

        // 6. Tickets (30 realistic items)
        var baseTime = DateTime.UtcNow.AddDays(-10);

        var sampleTicketsData = new (string Title, string? Description, Category Cat, TicketPriority Priority, TicketStatus Status, ApplicationUser Req, ApplicationUser? Assignee, int DaysAgo, bool SlaBreached)[]
        {
            ("Printer lantai 3 paper jam terus dan mengeluarkan bunyi berdecit saat mencetak dokumen lebih dari 5 lembar", "Printer sering macet saat mencetak berkas penagihan invoice akhir bulan.", catPrinter, TicketPriority.High, TicketStatus.InProgress, reqBambang, agentAgus, 2, false),
            ("Minta akses folder shared drive Procurement untuk tim audit internal", "Dibutuhkan akses baca ke folder shared drive Q3 Procurement untuk kepentingan audit operasional.", catAccess, TicketPriority.Medium, TicketStatus.Resolved, reqEko, agentRina, 5, false),
            ("VPN putus-putus saat WFH koneksi dari provider Indihome", null, catNetwork, TicketPriority.High, TicketStatus.Open, reqNurul, null, 1, false),
            ("Laptop baru untuk karyawan onboarding divisi Sales tanggal 17 November 2026", "Mohon dipersiapkan 1 unit ThinkPad beserta docking dan akun Google Workspace.", catHardware, TicketPriority.Low, TicketStatus.Open, reqDian, null, 3, false),
            ("Server database SAP ERP tidak dapat diakses dari seluruh cabang logistik Cikarang dan Surabaya sejak pagi ini", "Layanan ERP downtime menyebabkan antrean truk kontainer di gerbang gudang tidak bisa diverifikasi surat jalannya.", catSoftware, TicketPriority.Critical, TicketStatus.Closed, reqTri, agentFajar, 9, true),
            ("Monitor eksternal flickering hitam setiap 10 detik saat presentasi via HDMI", "Sudah diganti kabel HDMI baru tetapi keluhan berkedip tetap berulang.", catHardware, TicketPriority.Medium, TicketStatus.InProgress, reqBambang, agentSiti, 2, false),
            ("Reset password akun email korporat karena lupa sandi setelah cuti tahunan", null, catAccess, TicketPriority.High, TicketStatus.Resolved, reqNurul, agentAgus, 6, false),
            ("Outlook Web App gagal sinkronisasi kontak dan kalender rapat mingguan", "Kalender tidak menampilkan undangan rapat yang dikirim oleh divisi lain.", catEmail, TicketPriority.Medium, TicketStatus.Pending, reqEko, agentRina, 4, false),
            ("Keyboard laptop tombol spasi dan enter macet akibat ketumpahan cairan pembersih meja", "Tombol menempel dan sulit ditekan saat mengetik surat jalan pengiriman.", catHardware, TicketPriority.Low, TicketStatus.Closed, reqTri, agentFajar, 8, false),
            ("Koneksi WiFi area Warehouse Cikarang sering drop saat scanner barcode digunakan serentak", "Access point di lorong B tidak stabil saat jam sibuk shift pagi.", catNetwork, TicketPriority.High, TicketStatus.InProgress, reqTri, agentAgus, 4, true),
            ("Permintaan lisensi Microsoft Power BI Pro untuk pembuatan laporan bulanan Finance", "Dibutuhkan untuk mempublikasikan laporan performa keuangan ke direksi.", catSoftware, TicketPriority.Low, TicketStatus.Open, reqBambang, null, 2, false),
            ("Printer barcode di gudang penerimaan barang macet label tidak keluar", null, catPrinter, TicketPriority.High, TicketStatus.Resolved, reqTri, agentSiti, 7, false),
            ("Akun Windows terlockout setelah salah memasukkan PIN login 3 kali berturut-turut", null, catAccess, TicketPriority.Medium, TicketStatus.Closed, reqDian, agentFajar, 6, false),
            ("Tidak bisa mengirim email lampiran PDF ukuran 15 MB ke klien eksternal", "Muncul pesan error bounce email lampiran melebihi kuota pengiriman gateway.", catEmail, TicketPriority.Medium, TicketStatus.Open, reqNurul, null, 1, false),
            ("Permintaan penggantian baterai laptop yang drop hanya bertahan 20 menit tanpa charger", "Baterai kembung dan casing bawah laptop mulai renggang.", catHardware, TicketPriority.Low, TicketStatus.Pending, reqEko, agentRina, 3, false),
            ("Permohonan instalasi software Tableau Desktop pada workstation data analyst HR", "Installer sudah diunduh dari portal resmi, membutuhkan admin privilege untuk instalasi.", catSoftware, TicketPriority.Low, TicketStatus.Resolved, reqNurul, agentAgus, 8, false),
            ("Akses internet lantai 2 putus total seluruh staf departemen HR tidak bisa mengakses portal BPJS", "Kabel patch cord di switch rack ruang server terindikasi longgar atau rusak.", catNetwork, TicketPriority.Critical, TicketStatus.InProgress, reqNurul, agentFajar, 1, false),
            ("Permintaan perpanjangan akses VPN client untuk konsultan IT pihak ketiga proyek migrasi data", null, catAccess, TicketPriority.Medium, TicketStatus.Open, reqBambang, null, 2, false),
            ("Printer Epson L3110 di Finance hasil cetak bergaris dan warna magenta tidak keluar", "Sudah dilakukan head cleaning berkali-kali namun tinta tetap putus-putus.", catPrinter, TicketPriority.Low, TicketStatus.Open, reqBambang, null, 3, false),
            ("Pesan error 502 Bad Gateway saat membuka dashboard internal absensi karyawan", "Dibatalkan karena ternyata server sedang proses maintenance terjadwal oleh tim infrastruktur pusat.", catSoftware, TicketPriority.High, TicketStatus.Cancelled, reqDian, agentSiti, 5, false),
            ("Permohonan pengadaan webcam dan headset USB untuk keperluan meeting online departemen HR", null, catHardware, TicketPriority.Low, TicketStatus.Open, reqNurul, null, 2, false),
            ("Email phishing mengatasnamakan Bank Mandiri masuk ke inbox divisi Finance mohon segera diblokir", "Domain pengirim mencurigakan @bank-mandiri-update.com melampirkan file zip.", catEmail, TicketPriority.Critical, TicketStatus.Closed, reqBambang, agentAgus, 9, false),
            ("Aplikasi kalkulasi pajak SPT Masa PPh 21 crash saat import file CSV rekap gaji", "Error System.IndexOutOfRangeException muncul saat proses parsing baris ke-150.", catSoftware, TicketPriority.High, TicketStatus.InProgress, reqBambang, agentRina, 2, false),
            ("Pergantian toner hitam printer multifungsi lantai 1 departemen Procurement sudah menipis", null, catPrinter, TicketPriority.Medium, TicketStatus.Resolved, reqEko, agentSiti, 4, false),
            ("Kabel LAN putus terjepit laci meja kerja di ruang rapat utama lantai 4", "Konektor RJ45 patah dan kabel terkelupas.", catNetwork, TicketPriority.Medium, TicketStatus.Closed, reqDian, agentFajar, 7, false),
            ("Permintaan pembuatan email grup departemen finance-audit@perusahaan.co.id", "Anggota: Bambang Suryono, Rina Kartika, dan Hendra Wijaya.", catEmail, TicketPriority.Low, TicketStatus.Resolved, reqBambang, agentAgus, 8, false),
            ("Layar laptop blank hitam setelah update Windows 11 otomatis semalam", "Indikator power menyala dan kipas berputar tetapi tidak ada tampilan BIOS.", catHardware, TicketPriority.High, TicketStatus.Pending, reqDian, agentFajar, 3, false),
            ("Permintaan hak akses edit pada sheet Google Drive anggaran operasional cabang Cikarang", null, catAccess, TicketPriority.Medium, TicketStatus.Open, reqTri, null, 1, false),
            ("Peringatan disk space drive C server file server tersisa kurang dari 5 GB", "Banyak file temporary rendering dan backup log yang belum dirotasi otomatis.", catHardware, TicketPriority.Critical, TicketStatus.InProgress, reqTri, agentAgus, 3, true),
            ("Permintaan pemindahan unit PC dan konfigurasi IP address ke meja baru lantai 2 ruang sales", "Karyawan berpindah workstation karena reorganisasi tim penjualan regional.", catHardware, TicketPriority.Low, TicketStatus.Open, reqDian, null, 2, false)
        };

        var ticketList = new List<Ticket>();
        int ticketNumber = 1;

        foreach (var data in sampleTicketsData)
        {
            var createdDate = baseTime.AddDays(data.DaysAgo);
            var sla = data.Priority switch
            {
                TicketPriority.Critical => slaCritical,
                TicketPriority.High => slaHigh,
                TicketPriority.Medium => slaMedium,
                _ => slaLow
            };

            var responseDue = createdDate.AddMinutes(sla.ResponseTimeMinutes);
            var resolutionDue = createdDate.AddMinutes(sla.ResolutionTimeMinutes);

            DateTime? firstResponded = data.Assignee is not null ? createdDate.AddMinutes(15) : null;
            DateTime? resolvedAt = (data.Status == TicketStatus.Resolved || data.Status == TicketStatus.Closed)
                ? createdDate.AddHours(3)
                : null;
            DateTime? closedAt = data.Status == TicketStatus.Closed
                ? resolvedAt?.AddDays(3)
                : null;

            var ticket = new Ticket
            {
                TicketNo = $"HD-2026-{ticketNumber:D6}",
                Title = data.Title,
                Description = data.Description,
                CategoryId = data.Cat.Id,
                Priority = data.Priority,
                Status = data.Status,
                RequesterId = data.Req.Id,
                AssigneeId = data.Assignee?.Id,
                SlaPolicyId = sla.Id,
                ResponseDueAt = responseDue,
                ResolutionDueAt = resolutionDue,
                FirstRespondedAt = firstResponded,
                ResolvedAt = resolvedAt,
                ClosedAt = closedAt,
                IsSlaBreached = data.SlaBreached,
                CreatedAt = createdDate,
                CreatedBy = data.Req.FullName
            };

            // Comments
            ticket.Comments.Add(new TicketComment
            {
                AuthorId = data.Req.Id,
                Body = $"Tiket dibuat oleh {data.Req.FullName}.",
                IsInternal = false,
                CreatedAt = createdDate
            });

            if (data.Assignee is not null)
            {
                ticket.Comments.Add(new TicketComment
                {
                    AuthorId = data.Assignee.Id,
                    Body = $"Tiket telah diambil alih oleh {data.Assignee.FullName} untuk proses pengecekan teknis.",
                    IsInternal = false,
                    CreatedAt = createdDate.AddMinutes(20)
                });

                if (data.Status == TicketStatus.Pending)
                {
                    ticket.Comments.Add(new TicketComment
                    {
                        AuthorId = data.Assignee.Id,
                        Body = "Menunggu konfirmasi ketersediaan suku cadang dari vendor eksternal.",
                        IsInternal = true,
                        CreatedAt = createdDate.AddHours(2)
                    });
                }

                if (data.Status == TicketStatus.Resolved || data.Status == TicketStatus.Closed)
                {
                    ticket.Comments.Add(new TicketComment
                    {
                        AuthorId = data.Assignee.Id,
                        Body = "Masalah telah berhasil diperbaiki dan diverifikasi langsung bersama user.",
                        IsInternal = false,
                        CreatedAt = resolvedAt ?? createdDate.AddHours(3)
                    });
                }
            }

            // Histories
            ticket.Histories.Add(new TicketHistory
            {
                FieldName = "Status",
                OldValue = null,
                NewValue = "Open",
                ChangedById = data.Req.Id,
                ChangedAt = createdDate
            });

            if (data.Assignee is not null)
            {
                ticket.Histories.Add(new TicketHistory
                {
                    FieldName = "Assignee",
                    OldValue = null,
                    NewValue = data.Assignee.FullName,
                    ChangedById = data.Assignee.Id,
                    ChangedAt = createdDate.AddMinutes(20)
                });

                ticket.Histories.Add(new TicketHistory
                {
                    FieldName = "Status",
                    OldValue = "Open",
                    NewValue = "InProgress",
                    ChangedById = data.Assignee.Id,
                    ChangedAt = createdDate.AddMinutes(20)
                });
            }

            if (data.Status == TicketStatus.Resolved || data.Status == TicketStatus.Closed)
            {
                ticket.Histories.Add(new TicketHistory
                {
                    FieldName = "Status",
                    OldValue = "InProgress",
                    NewValue = "Resolved",
                    ChangedById = data.Assignee?.Id ?? admin.Id,
                    ChangedAt = resolvedAt ?? createdDate.AddHours(3)
                });
            }

            // Escalations for SLA breached tickets
            if (data.SlaBreached)
            {
                ticket.Escalations.Add(new Escalation
                {
                    Level = 1,
                    Reason = EscalationReason.SlaResolutionBreach,
                    Note = "Waktu resolusi tiket melebihi batas toleransi SLA.",
                    EscalatedToId = sup2.Id,
                    EscalatedById = null,
                    EscalatedAt = resolutionDue.AddMinutes(5),
                    AcknowledgedAt = null
                });
            }

            ticketList.Add(ticket);
            ticketNumber++;
        }

        _context.Tickets.AddRange(ticketList);
        await _context.SaveChangesAsync(cancellationToken);

        // 7. TicketNumberSequence
        var seq = new TicketNumberSequence
        {
            Year = 2026,
            LastNumber = ticketList.Count
        };
        _context.TicketNumberSequences.Add(seq);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeding database berhasil! {Count} tiket telah dibuat.", ticketList.Count);
    }
}

