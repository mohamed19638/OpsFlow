using OpsFlow.Domain.Entities;

namespace OpsFlow.Domain.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
}
