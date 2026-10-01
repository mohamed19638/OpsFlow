
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entities;
public class IncidentEvent
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public IncidentEventType EventType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }

    public Incident Incident { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}