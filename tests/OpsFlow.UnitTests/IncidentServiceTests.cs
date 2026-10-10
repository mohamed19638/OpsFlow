using FluentAssertions;
using Moq;
using OpsFlow.Application.DTOs.Incident;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Services;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.UnitTests;

public class IncidentServiceTests
{
    private readonly Mock<IUnitOfWork> _unit = new();
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IServiceRepository> _services = new();
    private readonly Mock<IIncidentRepository> _incidents = new();
    private readonly Mock<IIncidentEventRepository> _events = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IMessagePublisher> _publisher = new();
    private readonly IncidentService _service;
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _serviceId = Guid.NewGuid();
    private readonly Guid _incidentId = Guid.NewGuid();

    public IncidentServiceTests()
    {
        _unit.SetupGet(unit => unit.Organizations).Returns(_organizations.Object);
        _unit.SetupGet(unit => unit.Users).Returns(_users.Object);
        _unit.SetupGet(unit => unit.Services).Returns(_services.Object);
        _unit.SetupGet(unit => unit.Incidents).Returns(_incidents.Object);
        _unit.SetupGet(unit => unit.IncidentEvents).Returns(_events.Object);
        _unit.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _audit.Setup(audit => audit.LogAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publisher.Setup(publisher => publisher.PublishAsync(It.IsAny<string>(), It.IsAny<OpsFlow.Application.Messaging.IncidentCreatedMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _organizations.Setup(repo => repo.ExistsAsync(_orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _users.Setup(repo => repo.GetByIdAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _actorId, OrganizationId = _orgId });
        _service = new IncidentService(_unit.Object, _audit.Object, _publisher.Object);
    }

    [Fact]
    public async Task CreateAsync_trims_title_persists_event_and_publishes_message()
    {
        _services.Setup(repo => repo.GetByIdAsync(_serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Service { Id = _serviceId, OrganizationId = _orgId, Name = "API" });

        var result = await _service.CreateAsync(_orgId, new CreateIncidentDto
        {
            Title = "  API is down  ", Description = "Timeouts", Severity = "High", ServiceId = _serviceId
        }, _actorId);

        result.Title.Should().Be("API is down");
        result.Status.Should().Be(nameof(IncidentStatus.Detected));
        _incidents.Verify(repo => repo.AddAsync(It.Is<Incident>(incident =>
            incident.Title == "API is down" && incident.Events.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(publisher => publisher.PublishAsync("incidents.created", It.IsAny<OpsFlow.Application.Messaging.IncidentCreatedMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_rejects_service_from_another_organization()
    {
        _services.Setup(repo => repo.GetByIdAsync(_serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Service { Id = _serviceId, OrganizationId = Guid.NewGuid(), Name = "API" });

        var act = () => _service.CreateAsync(_orgId, new CreateIncidentDto
        {
            Title = "Issue", Description = "Details", Severity = "High", ServiceId = _serviceId
        }, _actorId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Service does not belong to this organization.");
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_severity()
    {
        var act = () => _service.CreateAsync(_orgId, new CreateIncidentDto
        {
            Title = "Issue", Description = "Details", Severity = "NotASeverity", ServiceId = _serviceId
        }, _actorId);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid severity: NotASeverity.");
    }

    [Fact]
    public async Task UpdateStatusAsync_rejects_invalid_transition()
    {
        SetTrackedIncident(IncidentStatus.Detected);

        var act = () => _service.UpdateStatusAsync(_orgId, _incidentId,
            new UpdateIncidentStatusDto { Status = nameof(IncidentStatus.Resolved) }, _actorId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot transition incident from Detected to Resolved.");
    }

    [Fact]
    public async Task UpdateStatusAsync_records_valid_transition()
    {
        var incident = SetTrackedIncident(IncidentStatus.Detected);

        var result = await _service.UpdateStatusAsync(_orgId, _incidentId,
            new UpdateIncidentStatusDto { Status = nameof(IncidentStatus.Investigating) }, _actorId);

        incident.Status.Should().Be(IncidentStatus.Investigating);
        result!.Status.Should().Be(nameof(IncidentStatus.Investigating));
        incident.Events.Should().ContainSingle();
        _unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_does_not_return_an_incident_from_another_organization()
    {
        _incidents.Setup(repo => repo.GetWithDetailsAsync(_incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Incident { Id = _incidentId, OrganizationId = Guid.NewGuid(), Title = "Private incident" });

        var result = await _service.GetByIdAsync(_orgId, _incidentId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSeverityAsync_records_new_severity()
    {
        var incident = SetTrackedIncident(IncidentStatus.Investigating);

        var result = await _service.UpdateSeverityAsync(_orgId, _incidentId,
            new UpdateIncidentSeverityDto { Severity = "Critical" }, _actorId);

        incident.Severity.Should().Be(IncidentSeverity.Critical);
        result!.Severity.Should().Be(nameof(IncidentSeverity.Critical));
        incident.Events.Should().ContainSingle();
    }

    [Fact]
    public async Task ResolveAsync_sets_resolution_timestamp_and_status()
    {
        var incident = SetTrackedIncident(IncidentStatus.Mitigating);

        var result = await _service.ResolveAsync(_orgId, _incidentId,
            new ResolveIncidentDto { ResolutionNotes = "Fixed the issue" }, _actorId);

        incident.Status.Should().Be(IncidentStatus.Resolved);
        incident.ResolvedAt.Should().NotBeNull();
        result!.Status.Should().Be(nameof(IncidentStatus.Resolved));
    }

    private Incident SetTrackedIncident(IncidentStatus status)
    {
        var incident = new Incident { Id = _incidentId, OrganizationId = _orgId, Status = status, Title = "Issue" };
        _incidents.Setup(repo => repo.GetByIdAsync(_incidentId, It.IsAny<CancellationToken>())).ReturnsAsync(incident);
        return incident;
    }
}
