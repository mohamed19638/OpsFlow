using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Interfaces;
using OpsFlow.Infrastructure.Data;


namespace OpsFlow.Infrastructure.Repositories;

public class OrganizationRepository : GenericRepository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(OpsFlowDbContext context)
        : base(context)
    {
    }
}
