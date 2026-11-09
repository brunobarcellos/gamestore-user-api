using GameStore.Users.Domain.Entities;
using GameStore.Users.Domain.ValueObjects;

namespace GameStore.Users.Domain.Repositories;

/// <summary>
/// Contrato de Repositório para a Raiz de Agregação <see cref="User"/>.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    void Remove(User user);
}

/// <summary>
/// Padrão Unit of Work para persistência transacional.
/// </summary>
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}

