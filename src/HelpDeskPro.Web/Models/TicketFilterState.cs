namespace HelpDeskPro.Web.Models;

using HelpDeskPro.Domain.Enums;

public class TicketFilterState
{
    public List<TicketStatus> Statuses { get; set; } = [];
    public List<TicketPriority> Priorities { get; set; } = [];
    public int? CategoryId { get; set; }
    public Guid? AssigneeId { get; set; }
    public bool Unassigned { get; set; }
    public string Search { get; set; } = string.Empty;
    public int PageSize { get; set; } = 20;
}

