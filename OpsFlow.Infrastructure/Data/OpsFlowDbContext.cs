using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Data;

public class OpsFlowDbContext : DbContext
{
    public OpsFlowDbContext(DbContextOptions<OpsFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentEvent> IncidentEvents => Set<IncidentEvent>();
    public DbSet<IncidentComment> IncidentComments => Set<IncidentComment>();
    public DbSet<Deployment> Deployments => Set<Deployment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organization
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.HasMany(x => x.Teams)
                .WithOne(x => x.Organization)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Users)
                .WithOne(x => x.Organization)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Services)
                .WithOne(x => x.Organization)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Incidents)
                .WithOne(x => x.Organization)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Team
        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("Teams");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Teams)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Role)
                .HasConversion<string>();

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.RefreshTokens)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TeamMember
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.ToTable("TeamMembers");

            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Team)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.TeamMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Token)
                .IsRequired();

            entity.HasIndex(x => x.Token)
                .IsUnique();

            entity.Ignore(x => x.IsExpired);
            entity.Ignore(x => x.IsRevoked);
            entity.Ignore(x => x.IsActive);
        });

        // Service
        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("Services");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Status)
                .HasConversion<string>();

            entity.HasOne(x => x.Team)
                .WithMany(x => x.Services)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Services)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Incident
        modelBuilder.Entity<Incident>(entity =>
        {
            entity.ToTable("Incidents");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.Description)
                .IsRequired();

            entity.Property(x => x.Severity)
                .HasConversion<string>();

            entity.Property(x => x.Status)
                .HasConversion<string>();

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Incidents)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Service)
                .WithMany(x => x.Incidents)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CreatedBy)
                .WithMany(x => x.CreatedIncidents)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedTo)
                .WithMany(x => x.AssignedIncidents)
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.AssignedToTeam)
                .WithMany()
                .HasForeignKey(x => x.AssignedToTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(x => x.Events)
                .WithOne(x => x.Incident)
                .HasForeignKey(x => x.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Comments)
                .WithOne(x => x.Incident)
                .HasForeignKey(x => x.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // IncidentEvent
        modelBuilder.Entity<IncidentEvent>(entity =>
        {
            entity.ToTable("IncidentEvents");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Description)
                .IsRequired();

            entity.Property(x => x.EventType)
                .HasConversion<string>();

            entity.HasOne(x => x.Incident)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // IncidentComment
        modelBuilder.Entity<IncidentComment>(entity =>
        {
            entity.ToTable("IncidentComments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Body)
                .IsRequired();

            entity.HasOne(x => x.Incident)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Deployment
        modelBuilder.Entity<Deployment>(entity =>
        {
            entity.ToTable("Deployments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Version)
                .IsRequired();

            entity.Property(x => x.Environment)
                .HasConversion<string>();

            entity.Property(x => x.Status)
                .HasConversion<string>();

            entity.HasOne(x => x.Service)
                .WithMany(x => x.Deployments)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DeployedBy)
                .WithMany()
                .HasForeignKey(x => x.DeployedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Details)
                .HasMaxLength(4000);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.CreatedAt);

            entity.HasIndex(x => new
            {
                x.EntityType,
                x.EntityId
            });
        });
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var modifiedEntities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.State == EntityState.Modified);

        foreach (var entry in modifiedEntities)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}