using FluentValidation;
using GameStore.Users.Application.Common.Interfaces;
using GameStore.Users.Application.DTOs;
using GameStore.Users.Application.Interfaces;
using GameStore.Users.Domain.Entities;
using GameStore.Users.Domain.Exceptions;
using GameStore.Users.Domain.Repositories;
using ValidationException = GameStore.Users.Domain.Exceptions.ValidationException;

namespace GameStore.Users.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _updateValidator = updateValidator;
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuário", id);
        }

        return MapToResponse(user);
    }

    public async Task<UserResponse> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Usuário não autenticado.");
        }

        return await GetByIdAsync(_currentUserService.UserId.Value, cancellationToken);
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapToResponse);
    }

    public async Task<UserResponse> UpdateProfileAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuário", id);
        }

        user.UpdateProfile(request.Name);
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToResponse(user);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuário", id);
        }

        user.Deactivate();
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuário", id);
        }

        user.Activate();
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email.Value,
            Role: user.Role,
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt
        );
    }
}

