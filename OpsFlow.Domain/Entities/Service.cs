using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
namespace OpsFlow.Domain.Entities;




public class Service : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceStatus Status { get; set; }
    public Guid? TeamId { get; set; }
    public Guid OrganizationId { get; set; }

    public Team? Team { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public ICollection<Deployment> Deployments { get; set; } = new List<Deployment>();
}