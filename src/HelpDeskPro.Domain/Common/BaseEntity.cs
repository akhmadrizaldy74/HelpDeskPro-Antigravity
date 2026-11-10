namespace HelpDeskPro.Domain.Common;

public abstract class BaseEntity<TId>
{
    public TId Id { get; set; } = default!;
}

public abstract class BaseEntity : BaseEntity<long>
{
}

