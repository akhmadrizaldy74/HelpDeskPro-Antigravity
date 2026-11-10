namespace HelpDeskPro.Domain.Entities;

using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string EmployeeNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public Guid? SupervisorId { get; set; }
    public bool IsActive { get; set; } = true;

    public Department Department { get; set; } = null!;
    public ApplicationUser? Supervisor { get; set; }
    public ICollection<ApplicationUser> Subordinates { get; set; } = new List<ApplicationUser>();
    public ICollection<Ticket> RequestedTickets { get; set; } = new List<Ticket>();
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
    public ICollection<Escalation> EscalationsReceived { get; set; } = new List<Escalation>();
    public ICollection<Escalation> EscalationsCreated { get; set; } = new List<Escalation>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

