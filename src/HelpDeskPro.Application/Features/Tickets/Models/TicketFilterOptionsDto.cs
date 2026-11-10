namespace HelpDeskPro.Application.Features.Tickets.Models;

public record CategoryLookupDto(int Id, string Name);
public record AssigneeLookupDto(Guid Id, string FullName);

public class TicketFilterOptionsDto
{
    public IReadOnlyList<CategoryLookupDto> Categories { get; set; } = [];
    public IReadOnlyList<AssigneeLookupDto> Assignees { get; set; } = [];
}

