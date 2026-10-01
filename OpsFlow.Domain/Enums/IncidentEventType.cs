namespace OpsFlow.Domain.Enums;

public enum IncidentEventType
{
    Created,
    StatusChange,
    SeverityChange,
    Assignment,
    Comment,
    Resolution,
    Reopened,
    Postmortem
}