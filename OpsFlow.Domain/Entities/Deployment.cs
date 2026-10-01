using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entities;
public class Deployment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ServiceId { get; set; }
    public string Version { get; set; } = string.Empty;
    public DeploymentEnvironment Environment { get; set; }
    public DeploymentStatus Status { get; set; }
    public Guid DeployedByUserId { get; set; }
    public DateTime DeployedAt { get; set; }
    public string? Notes { get; set; }

    public Service Service { get; set; } = null!;
    public User DeployedBy { get; set; } = null!;
}