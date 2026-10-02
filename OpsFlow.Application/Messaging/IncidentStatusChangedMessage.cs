namespace OpsFlow.Application.Messaging;

public sealed record IncidentStatusChangedMessage(
    Guid MessageId,
    Guid IncidentId,
    Guid OrganizationId,
    string OldStatus,
    string NewStatus);
