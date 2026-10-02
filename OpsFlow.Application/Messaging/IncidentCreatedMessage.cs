namespace OpsFlow.Application.Messaging;

public sealed record IncidentCreatedMessage(
    Guid MessageId,
    Guid IncidentId,
    Guid OrganizationId,
    string Title,
    string Severity,
    string ServiceName);
