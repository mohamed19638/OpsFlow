using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Settings;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Auth;
using OpsFlow.Infrastructure.Data;
using OpsFlow.Infrastructure.Repositories;

namespace OpsFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddDbContext<OpsFlowDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IIncidentEventRepository, IncidentEventRepository>();
        services.AddScoped<IIncidentCommentRepository, IncidentCommentRepository>();
        services.AddScoped<IDeploymentRepository, DeploymentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
