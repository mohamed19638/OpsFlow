using OpsFlow.Application.DTOs.Incident;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Mapping;

public static class IncidentMapping
{
    public static IncidentDto ToDto(this Incident incident) => new()
    {
        Id = incident.Id,
        OrganizationId = incident.OrganizationId,
        ServiceId = incident.ServiceId,
        Title = incident.Title,
        Description = incident.Description,
        Severity = incident.Severity.ToString(),
        Status = incident.Status.ToString(),
        CreatedByUserId = incident.CreatedByUserId,
        AssignedToUserId = incident.AssignedToUserId,
        AssignedToTeamId = incident.AssignedToTeamId,
        DetectedAt = incident.DetectedAt,
        ResolvedAt = incident.ResolvedAt,
        CreatedAt = incident.CreatedAt
    };

    public static IncidentDetailDto ToDetailDto(this Incident incident) => new()
    {
        Incident = incident.ToDto(),
        Events = incident.Events.OrderBy(item => item.CreatedAt).Select(item => item.ToDto()).ToList(),
        Comments = incident.Comments.OrderBy(item => item.CreatedAt).Select(item => item.ToDto()).ToList()
    };

    public static IncidentEventDto ToDto(this IncidentEvent item) => new()
    {
        Id = item.Id,
        IncidentId = item.IncidentId,
        CreatedByUserId = item.CreatedByUserId,
        EventType = item.EventType.ToString(),
        Description = item.Description,
        OldValue = item.OldValue,
        NewValue = item.NewValue,
        CreatedAt = item.CreatedAt
    };

    public static CommentDto ToDto(this IncidentComment item) => new()
    {
        Id = item.Id,
        IncidentId = item.IncidentId,
        UserId = item.UserId,
        Body = item.Body,
        CreatedAt = item.CreatedAt
    };
}
