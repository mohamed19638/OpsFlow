using OpsFlow.Domain.Enums;

using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entities;

public class Incident : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }

    public Guid ServiceId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? AssignedToTeamId { get; set; }

    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? PostmortemBody { get; set; }

    public Service Service { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public Team? AssignedToTeam { get; set; }

    public ICollection<IncidentEvent> Events { get; set; } = new List<IncidentEvent>();
    public ICollection<IncidentComment> Comments { get; set; } = new List<IncidentComment>();
}