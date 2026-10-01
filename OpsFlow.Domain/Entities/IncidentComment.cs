using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entities;

public class IncidentComment : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Guid UserId { get; set; }
    public string Body { get; set; } = string.Empty;

    public Incident Incident { get; set; } = null!;
    public User User { get; set; } = null!;
}