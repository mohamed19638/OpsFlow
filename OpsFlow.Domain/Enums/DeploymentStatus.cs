namespace OpsFlow.Domain.Enums;


public enum DeploymentStatus
{
    Pending,
    InProgress,
    Succeeded,
    Failed,
    RolledBack
}