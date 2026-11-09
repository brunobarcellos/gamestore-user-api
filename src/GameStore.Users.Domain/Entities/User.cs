using GameStore.Users.Domain.Common;
using GameStore.Users.Domain.Enums;
using GameStore.Users.Domain.Exceptions;
using GameStore.Users.Domain.ValueObjects;

namespace GameStore.Users.Domain.Entities;

/// <summary>
/// Entidade e Raiz de Agregação (Aggregate Root) do Usuário no microsserviço UserAPI.
/// Desacoplada do contexto de Catálogo/Biblioteca para isolamento de Bounded Context.
/// </summary>
public class User : Entity, IAggregateRoot
{
    public string Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Password Password { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected User() { }

    public User(
        string name,
        Email email,
        Password password,
        UserRole role = UserRole.User,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        ValidateName(name);

        Name = name.Trim();
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static User CreateAdmin(string name, Email email, Password password, Guid? id = null)
    {
        return new User(name, email, password, UserRole.Admin, id);
    }

    public void UpdateProfile(string name)
    {
        ValidateName(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(Password newPassword)
    {
        Password = newPassword ?? throw new ArgumentNullException(nameof(newPassword));
        UpdatedAt = DateTime.UtcNow;
    }

    public void PromoteToAdmin()
    {
        if (Role == UserRole.Admin)
            throw new ConflictException("O usuário já possui perfil de Administrador.");

        Role = UserRole.Admin;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DemoteToUser()
    {
        if (Role == UserRole.User)
            throw new ConflictException("O usuário já possui o perfil padrão de Usuário.");

        Role = UserRole.User;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new ConflictException("Usuário já está inativo.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            throw new ConflictException("Usuário já está ativo.");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException(nameof(Name), "O nome do usuário é obrigatório.");

        if (name.Trim().Length < 2 || name.Trim().Length > 100)
            throw new ValidationException(nameof(Name), "O nome do usuário deve conter entre 2 e 100 caracteres.");
    }
}

