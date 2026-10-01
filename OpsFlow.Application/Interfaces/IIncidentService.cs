using OpsFlow.Application.DTOs.Incident;

namespace OpsFlow.Application.Interfaces;

public interface IIncidentService
{
    Task<IReadOnlyList<IncidentDto>> GetByOrganizationAsync(
        Guid organizationId,
        string? severity = null,
        string? status = null,
        Guid? serviceId = null,
        CancellationToken cancellationToken = default);

    Task<IncidentDetailDto?> GetByIdAsync(
        Guid organizationId,
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<IncidentDto> CreateAsync(
        Guid organizationId,
        CreateIncidentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IncidentDto?> UpdateStatusAsync(
        Guid organizationId,
        Guid incidentId,
        UpdateIncidentStatusDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IncidentDto?> UpdateSeverityAsync(
        Guid organizationId,
        Guid incidentId,
        UpdateIncidentSeverityDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IncidentDto?> AssignAsync(
        Guid organizationId,
        Guid incidentId,
        AssignIncidentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<CommentDto?> AddCommentAsync(
        Guid organizationId,
        Guid incidentId,
        CreateCommentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentEventDto>?> GetTimelineAsync(
        Guid organizationId,
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<IncidentDto?> ResolveAsync(
        Guid organizationId,
        Guid incidentId,
        ResolveIncidentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IncidentDto?> ReopenAsync(
        Guid organizationId,
        Guid incidentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IncidentDto?> AddPostmortemAsync(
        Guid organizationId,
        Guid incidentId,
        PostmortemDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
