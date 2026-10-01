using OpsFlow.Application.DTOs.Organization;

namespace OpsFlow.Application.Interfaces;

public interface IOrganizationService
{
    Task<IReadOnlyList<OrganizationDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<OrganizationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OrganizationDto> CreateAsync(
        CreateOrganizationDto dto,
        CancellationToken cancellationToken = default);

    Task<OrganizationDto?> UpdateAsync(
        Guid id,
        UpdateOrganizationDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}