namespace HelpDeskPro.Domain.Entities;

using HelpDeskPro.Domain.Common;

public class Holiday : AuditableEntity<int>
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
}
