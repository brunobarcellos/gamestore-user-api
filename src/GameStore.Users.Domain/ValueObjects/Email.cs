using System.Text.RegularExpressions;
using GameStore.Users.Domain.Common;
using GameStore.Users.Domain.Exceptions;

namespace GameStore.Users.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor (Value Object) de E-mail de um usuário.
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException(nameof(Email), "E-mail não pode ser vazio.");

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (normalizedEmail.Length > 254)
            throw new ValidationException(nameof(Email), "E-mail não pode exceder 254 caracteres.");

        if (!EmailRegex.IsMatch(normalizedEmail))
            throw new ValidationException(nameof(Email), "E-mail inválido.");

        return new Email(normalizedEmail);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}

