using GameStore.Users.Domain.Enums;

namespace GameStore.Users.Application.DTOs;

public record RegisterUserRequest(
    string Name,
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string TokenType,
    DateTime ExpiresAt,
    UserInfoResponse User
);

public record UserInfoResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    DateTime CreatedAt
);

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpdateUserRequest(
    string Name
);

