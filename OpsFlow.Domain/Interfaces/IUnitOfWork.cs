namespace OpsFlow.Domain.Interfaces;

public interface IUnitOfWork
{
    IOrganizationRepository Organizations { get; }

    IUserRepository Users { get; }

    ITeamRepository Teams { get; }

    ITeamMemberRepository TeamMembers { get; }

    IServiceRepository Services { get; }

    IIncidentRepository Incidents { get; }

    IIncidentEventRepository IncidentEvents { get; }

    IIncidentCommentRepository IncidentComments { get; }

    IDeploymentRepository Deployments { get; }

    IAuditLogRepository AuditLogs { get; }

    IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
