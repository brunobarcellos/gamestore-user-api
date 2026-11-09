using FluentValidation;
using GameStore.Contracts.Events;
using GameStore.Users.Application.Common.Interfaces;
using GameStore.Users.Application.DTOs;
using GameStore.Users.Application.Interfaces;
using GameStore.Users.Domain.Entities;
using GameStore.Users.Domain.Exceptions;
using GameStore.Users.Domain.Repositories;
using GameStore.Users.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using ValidationException = GameStore.Users.Domain.Exceptions.ValidationException;

namespace GameStore.Users.Application.Services;

/// <summary>
/// Serviço de Aplicação responsável pelos Casos de Uso de Autenticação e Cadastro de Usuários.
/// Ao criar um novo usuário, publica o evento <see cref="UserCreatedEvent"/> na fila MQ.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEventPublisher _eventPublisher;
    private readonly IValidator<RegisterUserRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IEventPublisher eventPublisher,
        IValidator<RegisterUserRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _eventPublisher = eventPublisher;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        var email = Email.Create(request.Email);
        Password.ValidatePlainPassword(request.Password);

        var exists = await _userRepository.ExistsByEmailAsync(email, cancellationToken);
        if (exists)
        {
            throw new EmailAlreadyInUseException(request.Email);
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var passwordVo = Password.FromHash(passwordHash);

        var user = new User(request.Name, email, passwordVo);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Publica o evento UserCreatedEvent na fila MQ
        var userCreatedEvent = new UserCreatedEvent(
            UserId: user.Id,
            Name: user.Name,
            Email: user.Email.Value,
            CreatedAt: user.CreatedAt
        );

        await _eventPublisher.PublishAsync(userCreatedEvent, cancellationToken);
        _logger.LogInformation("Evento UserCreatedEvent publicado para o usuário {UserId} ({Email}).", user.Id, user.Email.Value);

        var token = _jwtTokenGenerator.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        return new AuthResponse(
            Token: token,
            TokenType: "Bearer",
            ExpiresAt: expiresAt,
            User: new UserInfoResponse(
                user.Id,
                user.Name,
                user.Email.Value,
                user.Role,
                user.CreatedAt
            )
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        var email = Email.Create(request.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("E-mail ou senha inválidos.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.Password.Hash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("E-mail ou senha inválidos.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        return new AuthResponse(
            Token: token,
            TokenType: "Bearer",
            ExpiresAt: expiresAt,
            User: new UserInfoResponse(
                user.Id,
                user.Name,
                user.Email.Value,
                user.Role,
                user.CreatedAt
            )
        );
    }
}

