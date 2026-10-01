using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Services;

namespace OpsFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IServiceService, ServiceManagementService>();
        services.AddScoped<IDeploymentService, DeploymentService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
