namespace HelpDeskPro.Domain.Entities;

using HelpDeskPro.Domain.Common;

public class Category : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

