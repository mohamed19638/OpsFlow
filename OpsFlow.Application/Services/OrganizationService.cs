using OpsFlow.Application.DTOs.Organization;
using OpsFlow.Application.Interfaces;
using OpsFlow.Application.Mapping;
using OpsFlow.Domain.Interfaces;

namespace OpsFlow.Application.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrganizationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<OrganizationDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var organizations = await _unitOfWork.Organizations
            .GetAllAsync(cancellationToken);

        return organizations
            .Select(organization => organization.ToDto())
            .ToList();
    }

    public async Task<OrganizationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organization = await _unitOfWork.Organizations
            .GetByIdAsync(id, cancellationToken);

        return organization?.ToDto();
    }

    public async Task<OrganizationDto> CreateAsync(
        CreateOrganizationDto dto,
        CancellationToken cancellationToken = default)
    {
        var organization = dto.ToEntity();

        await _unitOfWork.Organizations
            .AddAsync(organization, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return organization.ToDto();
    }

    public async Task<OrganizationDto?> UpdateAsync(
        Guid id,
        UpdateOrganizationDto dto,
        CancellationToken cancellationToken = default)
    {
        var organization = await _unitOfWork.Organizations
            .GetByIdAsync(id, cancellationToken);

        if (organization is null)
        {
            return null;
        }

        organization.UpdateEntity(dto);

        _unitOfWork.Organizations.Update(organization);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return organization.ToDto();
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organization = await _unitOfWork.Organizations
            .GetByIdAsync(id, cancellationToken);

        if (organization is null)
        {
            return false;
        }

        _unitOfWork.Organizations.Delete(organization);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
