using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Infrastructure.Repositories;
using OpsFlow.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly OpsFlowDbContext _context;

    public IOrganizationRepository Organizations { get; }
    public IUserRepository Users { get; }
    public ITeamRepository Teams { get; }
    public ITeamMemberRepository TeamMembers { get; }
    public IServiceRepository Services { get; }
    public IIncidentRepository Incidents { get; }
    public IIncidentEventRepository IncidentEvents { get; }
    public IIncidentCommentRepository IncidentComments { get; }
    public IDeploymentRepository Deployments { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IRefreshTokenRepository RefreshTokens { get; }

    public UnitOfWork(
        OpsFlowDbContext context,
        IOrganizationRepository organizations,
        IUserRepository users,
        ITeamRepository teams,
        ITeamMemberRepository teamMembers,
        IServiceRepository services,
        IIncidentRepository incidents,
        IIncidentEventRepository incidentEvents,
        IIncidentCommentRepository incidentComments,
        IDeploymentRepository deployments,
        IAuditLogRepository auditLogs,
        IRefreshTokenRepository refreshTokens)
    {
        _context = context;

        Organizations = organizations;
        Users = users;
        Teams = teams;
        TeamMembers = teamMembers;
        Services = services;
        Incidents = incidents;
        IncidentEvents = incidentEvents;
        IncidentComments = incidentComments;
        Deployments = deployments;
        AuditLogs = auditLogs;
        RefreshTokens = refreshTokens;
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
