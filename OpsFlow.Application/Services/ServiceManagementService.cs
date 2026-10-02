using OpsFlow.Application.DTOs.Service;
using OpsFlow.Application.Constants;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Mapping;
using OpsFlow.Domain.Enums;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Application.Services;

public class ServiceManagementService : IServiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public ServiceManagementService(IUnitOfWork unitOfWork, ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ServiceDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.ServicesByOrganization(organizationId);
        var cachedServices = await _cache.GetAsync<List<ServiceDto>>(cacheKey, cancellationToken);
        if (cachedServices is not null)
        {
            return cachedServices;
        }

        var services = await _unitOfWork.Services
            .GetByOrganizationAsync(
                organizationId,
                cancellationToken);

        var result = services
            .Select(service => service.ToDto())
            .ToList();

        await _cache.SetAsync(cacheKey, result, cancellationToken: cancellationToken);
        return result;
    }

    public async Task<ServiceDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services
            .GetByIdAsync(id, cancellationToken);

        return service?.ToDto();
    }

    public async Task<ServiceDto> CreateAsync(
        Guid organizationId,
        CreateServiceDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var organizationExists = await _unitOfWork.Organizations
            .ExistsAsync(organizationId, cancellationToken);

        if (!organizationExists)
        {
            throw new KeyNotFoundException(
                $"Organization {organizationId} not found.");
        }

        if (dto.TeamId.HasValue)
        {
            var team = await _unitOfWork.Teams
                .GetByIdAsync(dto.TeamId.Value, cancellationToken);

            if (team is null)
            {
                throw new KeyNotFoundException(
                    $"Team {dto.TeamId.Value} not found.");
            }

            if (team.OrganizationId != organizationId)
            {
                throw new InvalidOperationException(
                    "Team does not belong to this organization.");
            }

        }

        await EnsureTeamLeadOwnsTeamAsync(actorUserId, dto.TeamId, cancellationToken);

        var service = dto.ToEntity(organizationId);

        await _unitOfWork.Services
            .AddAsync(service, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(
            CacheKeys.ServicesByOrganization(organizationId),
            cancellationToken);

        return service.ToDto();
    }

    public async Task<ServiceDto?> UpdateAsync(
        Guid id,
        UpdateServiceDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services
            .GetByIdAsync(id, cancellationToken);

        if (service is null)
        {
            return null;
        }

        await EnsureTeamLeadOwnsTeamAsync(
            actorUserId,
            service.TeamId,
            cancellationToken);

        if (dto.TeamId.HasValue)
        {
            var team = await _unitOfWork.Teams
                .GetByIdAsync(dto.TeamId.Value, cancellationToken);

            if (team is null)
            {
                throw new KeyNotFoundException(
                    $"Team {dto.TeamId.Value} not found.");
            }

            if (team.OrganizationId != service.OrganizationId)
            {
                throw new InvalidOperationException(
                    "Team does not belong to this organization.");
            }

        }

        await EnsureTeamLeadOwnsTeamAsync(actorUserId, dto.TeamId, cancellationToken);

        service.UpdateEntity(dto);

        _unitOfWork.Services.Update(service);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(
            CacheKeys.ServicesByOrganization(service.OrganizationId),
            cancellationToken);

        return service.ToDto();
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services
            .GetByIdAsync(id, cancellationToken);

        if (service is null)
        {
            return false;
        }

        _unitOfWork.Services.Delete(service);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(
            CacheKeys.ServicesByOrganization(service.OrganizationId),
            cancellationToken);

        return true;
    }

    public async Task<ServiceDto?> UpdateStatusAsync(
        Guid id,
        UpdateServiceStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var service = await _unitOfWork.Services
            .GetByIdAsync(id, cancellationToken);

        if (service is null)
        {
            return null;
        }

        if (!Enum.TryParse(
                dto.Status,
                true,
                out Domain.Enums.ServiceStatus status))
        {
            throw new ArgumentException(
                $"Invalid service status: {dto.Status}");
        }

        service.Status = status;

        _unitOfWork.Services.Update(service);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(
            CacheKeys.ServicesByOrganization(service.OrganizationId),
            cancellationToken);

        return service.ToDto();
    }

    private async Task EnsureTeamLeadOwnsTeamAsync(
        Guid actorUserId,
        Guid? teamId,
        CancellationToken cancellationToken)
    {
        var actor = await _unitOfWork.Users.GetByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            throw new UnauthorizedAccessException("The current user could not be found.");
        }

        if (actor.Role == UserRole.Admin)
        {
            return;
        }

        if (actor.Role != UserRole.TeamLead ||
            !teamId.HasValue ||
            !await _unitOfWork.TeamMembers.ExistsByTeamAndUserAsync(
                teamId.Value,
                actorUserId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You must be a member of the service's team to manage it.");
        }
    }

}
