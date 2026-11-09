using FluentAssertions;
using FluentValidation;
using GameStore.Contracts.Events;
using GameStore.Users.Application.Common.Interfaces;
using GameStore.Users.Application.DTOs;
using GameStore.Users.Application.Services;
using GameStore.Users.Application.Validators;
using GameStore.Users.Domain.Entities;
using GameStore.Users.Domain.Enums;
using GameStore.Users.Domain.Exceptions;
using GameStore.Users.Domain.Repositories;
using GameStore.Users.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GameStore.Users.Tests;

public class UserAndAuthTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();
    private readonly IValidator<RegisterUserRequest> _registerValidator = new RegisterUserValidator();
    private readonly IValidator<LoginRequest> _loginValidator = new LoginValidator();

    private AuthService CreateSut()
    {
        return new AuthService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _eventPublisherMock.Object,
            _registerValidator,
            _loginValidator,
            NullLogger<AuthService>.Instance
        );
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserAndPublishUserCreatedEvent()
    {
        // Arrange
        var request = new RegisterUserRequest("Novo Jogador", "novo@gamestore.com", "Senha@123");

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock.Setup(h => h.HashPassword(request.Password))
            .Returns("bcrypt_hashed_password");

        _jwtTokenGeneratorMock.Setup(g => g.GenerateToken(It.IsAny<User>()))
            .Returns("jwt.token.generated");

        var sut = CreateSut();

        // Act
        var response = await sut.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Token.Should().Be("jwt.token.generated");
        response.User.Name.Should().Be("Novo Jogador");
        response.User.Email.Should().Be("novo@gamestore.com");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        _eventPublisherMock.Verify(p => p.PublishAsync(
            It.Is<UserCreatedEvent>(e =>
                e.UserId == response.User.Id &&
                e.Name == "Novo Jogador" &&
                e.Email == "novo@gamestore.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowEmailAlreadyInUseExceptionAndNotPublishEvent()
    {
        // Arrange
        var request = new RegisterUserRequest("Jogador", "existente@gamestore.com", "Senha@123");

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.RegisterAsync(request);
        await act.Should().ThrowAsync<EmailAlreadyInUseException>();

        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<UserCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponseWithToken()
    {
        // Arrange
        var request = new LoginRequest("admin@gamestore.com", "Admin@123456");
        var user = User.CreateAdmin("Admin", Email.Create(request.Email), Password.FromHash("hashed_admin_pass"));

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(h => h.VerifyPassword(request.Password, "hashed_admin_pass"))
            .Returns(true);

        _jwtTokenGeneratorMock.Setup(g => g.GenerateToken(user))
            .Returns("admin.jwt.token");

        var sut = CreateSut();

        // Act
        var response = await sut.LoginAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Token.Should().Be("admin.jwt.token");
        response.User.Role.Should().Be(UserRole.Admin);
    }
}

