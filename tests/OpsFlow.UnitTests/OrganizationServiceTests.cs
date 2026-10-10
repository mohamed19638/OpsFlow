using FluentAssertions;
using Moq;
using OpsFlow.Application.DTOs.Organization;
using OpsFlow.Application.Services;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.UnitTests;

public class OrganizationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly OrganizationService _service;

    public OrganizationServiceTests()
    {
        _unitOfWork.SetupGet(unit => unit.Organizations).Returns(_organizations.Object);
        _service = new OrganizationService(_unitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_maps_entities_to_dtos()
    {
        _organizations.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Organization>)[new() { Name = "Acme" }]);

        var result = await _service.GetAllAsync();

        result.Should().ContainSingle().Which.Name.Should().Be("Acme");
    }

    [Fact]
    public async Task GetByIdAsync_returns_mapped_organization_when_found()
    {
        var id = Guid.NewGuid();
        _organizations.Setup(repo => repo.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = id, Name = "Acme" });

        var result = await _service.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be("Acme");
    }

    [Fact]
    public async Task CreateAsync_persists_entity_and_returns_dto()
    {
        Organization? saved = null;
        _organizations.Setup(repo => repo.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Callback<Organization, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.CreateAsync(new CreateOrganizationDto { Name = "Acme", Description = "Ops" });

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Acme");
        result.Name.Should().Be("Acme");
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_returns_null_when_organization_does_not_exist()
    {
        _organizations.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateOrganizationDto { Name = "New name" });

        result.Should().BeNull();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_updates_existing_organization_and_saves()
    {
        var id = Guid.NewGuid();
        var organization = new Organization { Id = id, Name = "Old name" };
        _organizations.Setup(repo => repo.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.UpdateAsync(id, new UpdateOrganizationDto { Name = "New name", Description = "Updated" });

        result!.Name.Should().Be("New name");
        organization.Description.Should().Be("Updated");
        _organizations.Verify(repo => repo.Update(organization), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_returns_false_when_organization_does_not_exist()
    {
        _organizations.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        (await _service.DeleteAsync(Guid.NewGuid())).Should().BeFalse();
        _organizations.Verify(repo => repo.Delete(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_deletes_existing_organization_and_saves()
    {
        var organization = new Organization { Id = Guid.NewGuid(), Name = "Acme" };
        _organizations.Setup(repo => repo.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        (await _service.DeleteAsync(organization.Id)).Should().BeTrue();

        _organizations.Verify(repo => repo.Delete(organization), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
