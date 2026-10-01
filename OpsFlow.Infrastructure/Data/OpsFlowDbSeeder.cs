using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Infrastructure.Data;

public static class OpsFlowDbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        var logger  = scope.ServiceProvider.GetRequiredService<ILogger<OpsFlowDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            if (await context.Organizations.AnyAsync())
            {
                logger.LogInformation("Database already seeded — skipping.");
                return;
            }

            logger.LogInformation("Seeding database...");

            var now = DateTime.UtcNow;

            // ─── Stable IDs ───────────────────────────────────────────────────────
            var orgId    = Guid.NewGuid();
            var team1Id  = Guid.NewGuid(); // Backend
            var team2Id  = Guid.NewGuid(); // Frontend
            var team3Id  = Guid.NewGuid(); // Infra
            var adminId  = Guid.NewGuid();
            var lead1Id  = Guid.NewGuid();
            var lead2Id  = Guid.NewGuid();
            var eng1Id   = Guid.NewGuid();
            var eng2Id   = Guid.NewGuid();
            var eng3Id   = Guid.NewGuid();
            var viewerId = Guid.NewGuid();
            var svc1Id   = Guid.NewGuid(); // Auth Service
            var svc2Id   = Guid.NewGuid(); // API Gateway
            var svc3Id   = Guid.NewGuid(); // Web Dashboard
            var svc4Id   = Guid.NewGuid(); // Notification Service
            var inc1Id   = Guid.NewGuid();
            var inc2Id   = Guid.NewGuid();
            var dep1Id   = Guid.NewGuid();
            var dep2Id   = Guid.NewGuid();
            var dep3Id   = Guid.NewGuid();

            // ─── Organization ─────────────────────────────────────────────────────
            await context.Organizations.AddAsync(new Organization
            {
                Id          = orgId,
                Name        = "Acme Tech",
                Description = "Leading provider of cloud-native DevOps solutions.",
                CreatedAt   = now,
                UpdatedAt   = now
            });

            // ─── Teams ────────────────────────────────────────────────────────────
            await context.Teams.AddRangeAsync(
                new Team
                {
                    Id             = team1Id,
                    OrganizationId = orgId,
                    Name           = "Backend Team",
                    Description    = "Owns all server-side APIs and microservices.",
                    CreatedAt      = now,
                    UpdatedAt      = now
                },
                new Team
                {
                    Id             = team2Id,
                    OrganizationId = orgId,
                    Name           = "Frontend Team",
                    Description    = "Responsible for web and mobile interfaces.",
                    CreatedAt      = now,
                    UpdatedAt      = now
                },
                new Team
                {
                    Id             = team3Id,
                    OrganizationId = orgId,
                    Name           = "Infrastructure & DevOps",
                    Description    = "Manages cloud infrastructure, CI/CD, and deployments.",
                    CreatedAt      = now,
                    UpdatedAt      = now
                }
            );

            // ─── Users ────────────────────────────────────────────────────────────
            var admin = new User
            {
                Id             = adminId,
                OrganizationId = orgId,
                FirstName      = "Omar",
                LastName       = "Hassan",
                Email          = "admin@acmetech.io",
                Role           = UserRole.Admin,
                IsActive       = true,
                CreatedAt      = now,
                UpdatedAt      = now,
                PasswordHash   = BCrypt.Net.BCrypt.HashPassword("Admin@1234!")
            };

            var lead1 = new User
            {
                Id             = lead1Id,
                OrganizationId = orgId,
                FirstName      = "Sara",
                LastName       = "Khalil",
                Email          = "sara.khalil@acmetech.io",
                Role           = UserRole.TeamLead,
                IsActive       = true,
                CreatedAt      = now,
                UpdatedAt      = now,
                PasswordHash   = BCrypt.Net.BCrypt.HashPassword("Lead@1234!")
            };

            var lead2 = new User
            {
                Id             = lead2Id,
                OrganizationId = orgId,
                FirstName      = "Karim",
                LastName       = "Mostafa",
                Email          = "karim.mostafa@acmetech.io",
                Role           = UserRole.TeamLead,
                IsActive       = true,
                CreatedAt      = now,
                UpdatedAt      = now,
                PasswordHash   = BCrypt.Net.BCrypt.HashPassword("Lead@1234!")
            };

            var eng1 = new User
            {
                Id             = eng1Id,
                OrganizationId = orgId,
                FirstName      = "Nour",
                LastName       = "Ibrahim",
                Email          = "nour.ibrahim@acmetech.io",
                Role           = UserRole.Engineer,
                IsActive       = true,
                CreatedAt      = now,
                UpdatedAt      = now,
                PasswordHash   = BCrypt.Net.BCrypt.HashPassword("Eng@1234!")
            };

            var eng2 = new User
            {
                Id             = eng2Id,
                OrganizationId = orgId,
                FirstName      = "Ahmed",
                LastName       = "Sami",
                Email          = "ahmed.sami@acmetech.io",
                Role           = UserRole.Engineer,
                IsActive       = true,
                CreatedAt      = now,
                UpdatedAt      = now,
                PasswordHash   = BCrypt.Net.BCrypt.HashPassword("Eng@1234!")
            };

            var eng3 = new User
            {
                Id             = eng3Id,
                OrganizationId = orgId,
                FirstName      = "Laila",
                LastName       = "Youssef",
                Email          = "laila.youssef@acmetech.io",
                Role           = UserRole.Engineer,
                IsActive       = true,
                CreatedAt      = now,
                UpdatedAt      = now,
                PasswordHash   = BCrypt.Net.BCrypt.HashPassword("Eng@1234!")
            };

            var viewer = new User
            {
                Id             = viewerId,
                OrganizationId = orgId,
                FirstName      = "Mariam",
                LastName       = "Farid",
                Email          = "mariam.farid@acmetech.io",
                Role           = UserRole.Viewer,
                IsActive       = true,
                CreatedAt      = now,
                UpdatedAt      = now,
                PasswordHash   = BCrypt.Net.BCrypt.HashPassword("View@1234!")
            };

            await context.Users.AddRangeAsync(admin, lead1, lead2, eng1, eng2, eng3, viewer);

            // ─── Team Members ─────────────────────────────────────────────────────
            await context.TeamMembers.AddRangeAsync(
                new TeamMember { Id = Guid.NewGuid(), TeamId = team1Id, UserId = lead1Id, JoinedAt = now },
                new TeamMember { Id = Guid.NewGuid(), TeamId = team1Id, UserId = eng1Id,  JoinedAt = now },
                new TeamMember { Id = Guid.NewGuid(), TeamId = team1Id, UserId = eng2Id,  JoinedAt = now },
                new TeamMember { Id = Guid.NewGuid(), TeamId = team2Id, UserId = lead2Id, JoinedAt = now },
                new TeamMember { Id = Guid.NewGuid(), TeamId = team2Id, UserId = eng3Id,  JoinedAt = now },
                new TeamMember { Id = Guid.NewGuid(), TeamId = team3Id, UserId = adminId, JoinedAt = now }
            );

            // ─── Services ─────────────────────────────────────────────────────────
            await context.Services.AddRangeAsync(
                new Service
                {
                    Id             = svc1Id,
                    OrganizationId = orgId,
                    TeamId         = team1Id,
                    Name           = "Auth Service",
                    Description    = "Handles user authentication, JWT issuance, and refresh tokens.",
                    Status         = ServiceStatus.Operational,
                    CreatedAt      = now,
                    UpdatedAt      = now
                },
                new Service
                {
                    Id             = svc2Id,
                    OrganizationId = orgId,
                    TeamId         = team1Id,
                    Name           = "API Gateway",
                    Description    = "Central routing — rate limiting, auth middleware, load balancing.",
                    Status         = ServiceStatus.Operational,
                    CreatedAt      = now,
                    UpdatedAt      = now
                },
                new Service
                {
                    Id             = svc3Id,
                    OrganizationId = orgId,
                    TeamId         = team2Id,
                    Name           = "Web Dashboard",
                    Description    = "React-based frontend for operations monitoring.",
                    Status         = ServiceStatus.Degraded,
                    CreatedAt      = now,
                    UpdatedAt      = now
                },
                new Service
                {
                    Id             = svc4Id,
                    OrganizationId = orgId,
                    TeamId         = team3Id,
                    Name           = "Notification Service",
                    Description    = "Sends email, Slack, and push alerts for incidents and deployments.",
                    Status         = ServiceStatus.Operational,
                    CreatedAt      = now,
                    UpdatedAt      = now
                }
            );

            // ─── Deployments ──────────────────────────────────────────────────────
            await context.Deployments.AddRangeAsync(
                new Deployment
                {
                    Id               = dep1Id,
                    OrganizationId   = orgId,
                    ServiceId        = svc1Id,
                    Version          = "v2.4.1",
                    Environment      = DeploymentEnvironment.Production,
                    Status           = DeploymentStatus.Succeeded,
                    DeployedByUserId = lead1Id,
                    DeployedAt       = now.AddDays(-7),
                    Notes            = "Stable release — added OAuth2 PKCE support.",
                    CreatedAt        = now.AddDays(-7),
                    UpdatedAt        = now.AddDays(-7)
                },
                new Deployment
                {
                    Id               = dep2Id,
                    OrganizationId   = orgId,
                    ServiceId        = svc2Id,
                    Version          = "v1.9.0",
                    Environment      = DeploymentEnvironment.Staging,
                    Status           = DeploymentStatus.InProgress,
                    DeployedByUserId = adminId,
                    DeployedAt       = now.AddHours(-2),
                    Notes            = "Testing new rate-limiting algorithm before prod push.",
                    CreatedAt        = now.AddHours(-2),
                    UpdatedAt        = now.AddHours(-2)
                },
                new Deployment
                {
                    Id               = dep3Id,
                    OrganizationId   = orgId,
                    ServiceId        = svc3Id,
                    Version          = "v3.1.0-beta",
                    Environment      = DeploymentEnvironment.Development,
                    Status           = DeploymentStatus.Failed,
                    DeployedByUserId = eng3Id,
                    DeployedAt       = now.AddDays(-1),
                    Notes            = "Build failed — missing env variable REACT_APP_API_URL.",
                    CreatedAt        = now.AddDays(-1),
                    UpdatedAt        = now.AddDays(-1)
                }
            );

            // ─── Incidents ────────────────────────────────────────────────────────
            await context.Incidents.AddRangeAsync(
                new Incident
                {
                    Id               = inc1Id,
                    OrganizationId   = orgId,
                    ServiceId        = svc3Id,
                    CreatedByUserId  = eng1Id,
                    AssignedToUserId = lead2Id,
                    AssignedToTeamId = team2Id,
                    Title            = "Dashboard loading latency spike",
                    Description      = "Users reporting 8-12s load times on the main dashboard. P95 latency up 400% since last deployment.",
                    Severity         = IncidentSeverity.High,
                    Status           = IncidentStatus.Investigating,
                    DetectedAt       = now.AddHours(-3),
                    CreatedAt        = now.AddHours(-3),
                    UpdatedAt        = now.AddHours(-1)
                },
                new Incident
                {
                    Id               = inc2Id,
                    OrganizationId   = orgId,
                    ServiceId        = svc1Id,
                    CreatedByUserId  = adminId,
                    AssignedToUserId = lead1Id,
                    AssignedToTeamId = team1Id,
                    Title            = "Auth token refresh loop causing 401s",
                    Description      = "~5% of users hitting a refresh token loop resulting in continuous 401 Unauthorized errors.",
                    Severity         = IncidentSeverity.Critical,
                    Status           = IncidentStatus.Resolved,
                    DetectedAt       = now.AddDays(-3),
                    ResolvedAt       = now.AddDays(-2),
                    PostmortemBody   = "Root cause: race condition in token rotation logic. Fixed in v2.4.1 with distributed locking.",
                    CreatedAt        = now.AddDays(-3),
                    UpdatedAt        = now.AddDays(-2)
                }
            );

            // ─── Incident Events ──────────────────────────────────────────────────
            await context.IncidentEvents.AddRangeAsync(
                new IncidentEvent
                {
                    Id              = Guid.NewGuid(),
                    IncidentId      = inc1Id,
                    CreatedByUserId = eng1Id,
                    EventType       = IncidentEventType.Created,
                    Description     = "Incident detected. Status set to Investigating.",
                    CreatedAt       = now.AddHours(-3)
                },
                new IncidentEvent
                {
                    Id              = Guid.NewGuid(),
                    IncidentId      = inc1Id,
                    CreatedByUserId = lead2Id,
                    EventType       = IncidentEventType.Assignment,
                    Description     = "Assigned to Frontend Team Lead Karim Mostafa.",
                    OldValue        = null,
                    NewValue        = lead2Id.ToString(),
                    CreatedAt       = now.AddHours(-2)
                },
                new IncidentEvent
                {
                    Id              = Guid.NewGuid(),
                    IncidentId      = inc2Id,
                    CreatedByUserId = lead1Id,
                    EventType       = IncidentEventType.StatusChange,
                    Description     = "Root cause identified — race condition in token rotation.",
                    OldValue        = nameof(IncidentStatus.Investigating),
                    NewValue        = nameof(IncidentStatus.Identified),
                    CreatedAt       = now.AddDays(-3).AddHours(2)
                },
                new IncidentEvent
                {
                    Id              = Guid.NewGuid(),
                    IncidentId      = inc2Id,
                    CreatedByUserId = lead1Id,
                    EventType       = IncidentEventType.Resolution,
                    Description     = "Fix deployed in v2.4.1. Incident resolved.",
                    OldValue        = nameof(IncidentStatus.Mitigating),
                    NewValue        = nameof(IncidentStatus.Resolved),
                    CreatedAt       = now.AddDays(-2)
                }
            );

            // ─── Incident Comments ────────────────────────────────────────────────
            await context.IncidentComments.AddRangeAsync(
                new IncidentComment
                {
                    Id         = Guid.NewGuid(),
                    IncidentId = inc1Id,
                    UserId     = eng3Id,
                    Body       = "Checked CDN logs — no anomalies. Likely React bundle size regression from v3.1.0-beta.",
                    CreatedAt  = now.AddHours(-2),
                    UpdatedAt  = now.AddHours(-2)
                },
                new IncidentComment
                {
                    Id         = Guid.NewGuid(),
                    IncidentId = inc1Id,
                    UserId     = lead2Id,
                    Body       = "Rolling back v3.1.0-beta. Bundle regression to be investigated separately.",
                    CreatedAt  = now.AddHours(-1),
                    UpdatedAt  = now.AddHours(-1)
                },
                new IncidentComment
                {
                    Id         = Guid.NewGuid(),
                    IncidentId = inc2Id,
                    UserId     = eng1Id,
                    Body       = "Fix confirmed live. 401 error rate back to baseline (0.01%) after 30 min monitoring.",
                    CreatedAt  = now.AddDays(-2).AddHours(1),
                    UpdatedAt  = now.AddDays(-2).AddHours(1)
                }
            );

            // ─── Audit Logs ───────────────────────────────────────────────────────
            await context.AuditLogs.AddRangeAsync(
                new AuditLog
                {
                    Id             = Guid.NewGuid(),
                    OrganizationId = orgId,
                    UserId         = adminId,
                    Action         = "CREATE",
                    EntityType     = nameof(Organization),
                    EntityId       = orgId,
                    Details        = "Organization 'Acme Tech' created.",
                    CreatedAt      = now
                },
                new AuditLog
                {
                    Id             = Guid.NewGuid(),
                    OrganizationId = orgId,
                    UserId         = adminId,
                    Action         = "CREATE",
                    EntityType     = nameof(User),
                    EntityId       = lead1Id,
                    Details        = "User sara.khalil@acmetech.io created with role TeamLead.",
                    CreatedAt      = now
                },
                new AuditLog
                {
                    Id             = Guid.NewGuid(),
                    OrganizationId = orgId,
                    UserId         = adminId,
                    Action         = "DEPLOY",
                    EntityType     = nameof(Deployment),
                    EntityId       = dep1Id,
                    Details        = "Auth Service v2.4.1 deployed to Production successfully.",
                    CreatedAt      = now.AddDays(-7)
                },
                new AuditLog
                {
                    Id             = Guid.NewGuid(),
                    OrganizationId = orgId,
                    UserId         = eng1Id,
                    Action         = "CREATE",
                    EntityType     = nameof(Incident),
                    EntityId       = inc1Id,
                    Details        = "Incident 'Dashboard loading latency spike' opened — severity High.",
                    CreatedAt      = now.AddHours(-3)
                },
                new AuditLog
                {
                    Id             = Guid.NewGuid(),
                    OrganizationId = orgId,
                    UserId         = lead1Id,
                    Action         = "RESOLVE",
                    EntityType     = nameof(Incident),
                    EntityId       = inc2Id,
                    Details        = "Incident 'Auth token refresh loop' resolved. Postmortem added.",
                    CreatedAt      = now.AddDays(-2)
                }
            );

            await context.SaveChangesAsync();
            logger.LogInformation("Database seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
