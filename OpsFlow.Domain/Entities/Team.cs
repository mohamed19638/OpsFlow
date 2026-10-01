using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entities;
public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}