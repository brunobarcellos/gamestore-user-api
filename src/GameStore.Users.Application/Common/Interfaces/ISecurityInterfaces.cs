using GameStore.Users.Domain.Entities;

namespace GameStore.Users.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string plainPassword);
    bool VerifyPassword(string plainPassword, string passwordHash);
}

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? UserRole { get; }
    bool IsAuthenticated { get; }
}

/// <summary>
/// Abstração para publicação de eventos de integração no barramento de mensageria (RabbitMQ).
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}

