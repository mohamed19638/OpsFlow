using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Entities;
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}