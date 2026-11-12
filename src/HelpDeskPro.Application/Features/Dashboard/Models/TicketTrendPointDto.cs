namespace HelpDeskPro.Application.Features.Dashboard.Models;

/// <summary>
/// Jumlah tiket masuk dan selesai per tanggal (zona waktu WIB).
/// </summary>
public sealed record TicketTrendPointDto(DateOnly Date, int CreatedCount, int ResolvedCount);
