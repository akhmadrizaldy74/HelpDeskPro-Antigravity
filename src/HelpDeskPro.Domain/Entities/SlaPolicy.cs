namespace HelpDeskPro.Domain.Entities;

using HelpDeskPro.Domain.Common;
using HelpDeskPro.Domain.Enums;

public class SlaPolicy : BaseEntity<int>
{
    public TicketPriority Priority { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

