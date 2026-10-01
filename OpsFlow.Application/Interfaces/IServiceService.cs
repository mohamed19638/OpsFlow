using OpsFlow.Application.DTOs.Service;

namespace OpsFlow.Application.Interfaces;

public interface IServiceService
{
    Task<IReadOnlyList<ServiceDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ServiceDto> CreateAsync(
        Guid organizationId,
        CreateServiceDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ServiceDto?> UpdateAsync(
        Guid id,
        UpdateServiceDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ServiceDto?> UpdateStatusAsync(
        Guid id,
        UpdateServiceStatusDto dto,
        CancellationToken cancellationToken = default);
}
