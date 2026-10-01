using OpsFlow.Application.DTOs.Incident;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Mapping;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Helpers;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Application.Services;

public class IncidentService : IIncidentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public IncidentService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<IncidentDto>> GetByOrganizationAsync(
        Guid organizationId,
        string? severity = null,
        string? status = null,
        Guid? serviceId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureOrganizationExistsAsync(organizationId, cancellationToken);
        IncidentSeverity? parsedSeverity = severity is null ? null : ParseEnum<IncidentSeverity>(severity, "severity");
        IncidentStatus? parsedStatus = status is null ? null : ParseEnum<IncidentStatus>(status, "status");
        var incidents = await _unitOfWork.Incidents.GetByOrganizationAsync(organizationId, cancellationToken);

        return incidents
            .Where(incident => !parsedSeverity.HasValue || incident.Severity == parsedSeverity)
            .Where(incident => !parsedStatus.HasValue || incident.Status == parsedStatus)
            .Where(incident => !serviceId.HasValue || incident.ServiceId == serviceId)
            .Select(incident => incident.ToDto())
            .ToList();
    }

    public async Task<IncidentDetailDto?> GetByIdAsync(
        Guid organizationId,
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident = await _unitOfWork.Incidents.GetWithDetailsAsync(incidentId, cancellationToken);
        return incident is null || incident.OrganizationId != organizationId
            ? null
            : incident.ToDetailDto();
    }

    public async Task<IncidentDto> CreateAsync(
        Guid organizationId,
        CreateIncidentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureOrganizationExistsAsync(organizationId, cancellationToken);
        var severity = ParseEnum<IncidentSeverity>(dto.Severity, "severity");
        var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service {dto.ServiceId} not found.");

        if (service.OrganizationId != organizationId)
            throw new InvalidOperationException("Service does not belong to this organization.");

        await EnsureActorBelongsToOrganizationAsync(actorUserId, organizationId, cancellationToken);
        var now = DateTime.UtcNow;
        var incident = new Incident
        {
            OrganizationId = organizationId,
            ServiceId = dto.ServiceId,
            CreatedByUserId = actorUserId,
            Title = dto.Title.Trim(),
            Description = dto.Description,
            Severity = severity,
            Status = IncidentStatus.Detected,
            DetectedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _unitOfWork.Incidents.AddAsync(incident, cancellationToken);
        AddEvent(incident, actorUserId, IncidentEventType.Created, "Incident created", null, IncidentStatus.Detected.ToString(), now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(incident, actorUserId, "IncidentCreated", $"Incident '{incident.Title}' was created.", cancellationToken);
        return incident.ToDto();
    }

    public async Task<IncidentDto?> UpdateStatusAsync(
        Guid organizationId,
        Guid incidentId,
        UpdateIncidentStatusDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetTrackedIncidentAsync(organizationId, incidentId, cancellationToken, actorUserId);
        if (incident is null) return null;
        EnsureNotClosed(incident);
        var newStatus = ParseEnum<IncidentStatus>(dto.Status, "status");
        if (!IncidentStateMachine.CanTransition(incident.Status, newStatus))
            throw new InvalidOperationException($"Cannot transition incident from {incident.Status} to {newStatus}.");

        var oldStatus = incident.Status;
        incident.Status = newStatus;
        var now = DateTime.UtcNow;
        AddEvent(incident, actorUserId, IncidentEventType.StatusChange, $"Status changed from {oldStatus} to {newStatus}.", oldStatus.ToString(), newStatus.ToString(), now);
        await SaveAndLogAsync(incident, actorUserId, "IncidentStatusChanged", $"Status: {oldStatus} -> {newStatus}.", cancellationToken);
        return incident.ToDto();
    }

    public async Task<IncidentDto?> UpdateSeverityAsync(
        Guid organizationId,
        Guid incidentId,
        UpdateIncidentSeverityDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetTrackedIncidentAsync(organizationId, incidentId, cancellationToken, actorUserId);
        if (incident is null) return null;
        EnsureNotClosed(incident);
        var newSeverity = ParseEnum<IncidentSeverity>(dto.Severity, "severity");
        var oldSeverity = incident.Severity;
        incident.Severity = newSeverity;
        var now = DateTime.UtcNow;
        AddEvent(incident, actorUserId, IncidentEventType.SeverityChange, $"Severity changed from {oldSeverity} to {newSeverity}.", oldSeverity.ToString(), newSeverity.ToString(), now);
        await SaveAndLogAsync(incident, actorUserId, "IncidentSeverityChanged", $"Severity: {oldSeverity} -> {newSeverity}.", cancellationToken);
        return incident.ToDto();
    }

    public async Task<IncidentDto?> AssignAsync(
        Guid organizationId,
        Guid incidentId,
        AssignIncidentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetTrackedIncidentAsync(organizationId, incidentId, cancellationToken, actorUserId);
        if (incident is null) return null;
        EnsureNotClosed(incident);
        if (dto.UserId.HasValue && dto.TeamId.HasValue)
            throw new ArgumentException("Assign the incident to either a user or a team, not both.");

        if (dto.UserId.HasValue)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"User {dto.UserId.Value} not found.");
            if (user.OrganizationId != organizationId)
                throw new InvalidOperationException("User does not belong to this organization.");
        }

        if (dto.TeamId.HasValue)
        {
            var team = await _unitOfWork.Teams.GetByIdAsync(dto.TeamId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Team {dto.TeamId.Value} not found.");
            if (team.OrganizationId != organizationId)
                throw new InvalidOperationException("Team does not belong to this organization.");
        }

        var oldAssignment = FormatAssignment(incident.AssignedToUserId, incident.AssignedToTeamId);
        incident.AssignedToUserId = dto.UserId;
        incident.AssignedToTeamId = dto.TeamId;
        var newAssignment = FormatAssignment(dto.UserId, dto.TeamId);
        var now = DateTime.UtcNow;
        AddEvent(incident, actorUserId, IncidentEventType.Assignment, $"Incident assignment changed to {newAssignment}.", oldAssignment, newAssignment, now);
        await SaveAndLogAsync(incident, actorUserId, "IncidentAssigned", $"Assignment: {oldAssignment} -> {newAssignment}.", cancellationToken);
        return incident.ToDto();
    }

    public async Task<CommentDto?> AddCommentAsync(
        Guid organizationId,
        Guid incidentId,
        CreateCommentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetTrackedIncidentAsync(organizationId, incidentId, cancellationToken, actorUserId);
        if (incident is null) return null;
        EnsureNotClosed(incident);
        var now = DateTime.UtcNow;
        var comment = new IncidentComment
        {
            IncidentId = incidentId,
            UserId = actorUserId,
            Body = dto.Body.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        await _unitOfWork.IncidentComments.AddAsync(comment, cancellationToken);
        AddEvent(incident, actorUserId, IncidentEventType.Comment, "Comment added", null, null, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(incident, actorUserId, "IncidentCommentAdded", "A comment was added.", cancellationToken);
        return comment.ToDto();
    }

    public async Task<IReadOnlyList<IncidentEventDto>?> GetTimelineAsync(
        Guid organizationId,
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetTrackedIncidentAsync(organizationId, incidentId, cancellationToken);
        if (incident is null) return null;
        var events = await _unitOfWork.IncidentEvents.GetByIncidentAsync(incidentId, cancellationToken);
        return events.OrderBy(item => item.CreatedAt).Select(item => item.ToDto()).ToList();
    }

    public async Task<IncidentDto?> ResolveAsync(
        Guid organizationId,
        Guid incidentId,
        ResolveIncidentDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetTrackedIncidentAsync(organizationId, incidentId, cancellationToken, actorUserId);
        if (incident is null) return null;
        if (incident.Status is IncidentStatus.Resolved or IncidentStatus.Postmortem or IncidentStatus.Closed)
            throw new InvalidOperationException($"An incident in {incident.Status} status cannot be resolved.");

        var oldStatus = incident.Status;
        incident.Status = IncidentStatus.Resolved;
        incident.ResolvedAt = DateTime.UtcNow;
        AddEvent(incident, actorUserId, IncidentEventType.Resolution, $"Incident resolved: {dto.ResolutionNotes.Trim()}", oldStatus.ToString(), IncidentStatus.Resolved.ToString(), incident.ResolvedAt.Value);
        await SaveAndLogAsync(incident, actorUserId, "IncidentResolved", dto.ResolutionNotes.Trim(), cancellationToken);
        return incident.ToDto();
    }

    public async Task<IncidentDto?> ReopenAsync(
        Guid organizationId,
        Guid incidentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetTrackedIncidentAsync(organizationId, incidentId, cancellationToken, actorUserId);
        if (incident is null) return null;
        if (incident.Status != IncidentStatus.Resolved)
            throw new InvalidOperationException($"Only resolved incidents can be reopened; current status is {incident.Status}.");

        var oldStatus = incident.Status;
        incident.Status = IncidentStatus.Investigating;
        incident.ResolvedAt = null;
        var now = DateTime.UtcNow;
        AddEvent(incident, actorUserId, IncidentEventType.Reopened, "Incident reopened", oldStatus.ToString(), IncidentStatus.Investigating.ToString(), now);
        await SaveAndLogAsync(incident, actorUserId, "IncidentReopened", $"Status: {oldStatus} -> {IncidentStatus.Investigating}.", cancellationToken);
        return incident.ToDto();
    }

    public async Task<IncidentDto?> AddPostmortemAsync(
        Guid organizationId,
        Guid incidentId,
        PostmortemDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var incident = await GetTrackedIncidentAsync(organizationId, incidentId, cancellationToken, actorUserId);
        if (incident is null) return null;
        if (incident.Status != IncidentStatus.Resolved)
            throw new InvalidOperationException("A postmortem can only be added to a resolved incident.");

        incident.Status = IncidentStatus.Postmortem;
        incident.PostmortemBody = dto.Body.Trim();
        var now = DateTime.UtcNow;
        AddEvent(incident, actorUserId, IncidentEventType.Postmortem, "Postmortem added", IncidentStatus.Resolved.ToString(), IncidentStatus.Postmortem.ToString(), now);
        await SaveAndLogAsync(incident, actorUserId, "IncidentPostmortemAdded", "Postmortem was added.", cancellationToken);
        return incident.ToDto();
    }

    private async Task<Incident?> GetTrackedIncidentAsync(
        Guid organizationId,
        Guid incidentId,
        CancellationToken cancellationToken,
        Guid? actorUserId = null)
    {
        if (actorUserId.HasValue)
            await EnsureActorBelongsToOrganizationAsync(actorUserId.Value, organizationId, cancellationToken);

        var incident = await _unitOfWork.Incidents.GetByIdAsync(incidentId, cancellationToken);
        return incident is null || incident.OrganizationId != organizationId ? null : incident;
    }

    private async Task EnsureOrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Organizations.ExistsAsync(organizationId, cancellationToken))
            throw new KeyNotFoundException($"Organization {organizationId} not found.");
    }

    private async Task EnsureActorBelongsToOrganizationAsync(Guid actorUserId, Guid organizationId, CancellationToken cancellationToken)
    {
        var actor = await _unitOfWork.Users.GetByIdAsync(actorUserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {actorUserId} not found.");
        if (actor.OrganizationId != organizationId)
            throw new InvalidOperationException("User does not belong to this organization.");
    }

    private async Task SaveAndLogAsync(Incident incident, Guid actorUserId, string action, string details, CancellationToken cancellationToken)
    {
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(incident, actorUserId, action, details, cancellationToken);
    }

    private Task LogAuditAsync(Incident incident, Guid actorUserId, string action, string details, CancellationToken cancellationToken) =>
        _auditLogService.LogAsync(incident.OrganizationId, actorUserId, action, nameof(Incident), incident.Id, details, cancellationToken);

    private static void AddEvent(Incident incident, Guid actorUserId, IncidentEventType eventType, string description, string? oldValue, string? newValue, DateTime createdAt)
    {
        incident.Events.Add(new IncidentEvent
        {
            IncidentId = incident.Id,
            CreatedByUserId = actorUserId,
            EventType = eventType,
            Description = description,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = createdAt
        });
    }

    private static void EnsureNotClosed(Incident incident)
    {
        if (incident.Status == IncidentStatus.Closed)
            throw new InvalidOperationException("A closed incident cannot be modified.");
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value, true, out var parsed) || !Enum.IsDefined(parsed))
            throw new ArgumentException($"Invalid {fieldName}: {value}.");
        return parsed;
    }

    private static string FormatAssignment(Guid? userId, Guid? teamId) =>
        userId.HasValue ? $"user:{userId.Value}" : teamId.HasValue ? $"team:{teamId.Value}" : "unassigned";
}
