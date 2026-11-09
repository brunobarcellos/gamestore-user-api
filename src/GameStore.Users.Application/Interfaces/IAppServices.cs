using GameStore.Users.Application.DTOs;

namespace GameStore.Users.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserResponse> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateProfileAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
}

