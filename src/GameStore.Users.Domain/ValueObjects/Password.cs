using System.Text.RegularExpressions;
using GameStore.Users.Domain.Common;
using GameStore.Users.Domain.Exceptions;

namespace GameStore.Users.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor (Value Object) para Senhas de Usuários.
/// </summary>
public sealed class Password : ValueObject
{
    private static readonly Regex LetterRegex = new(@"[a-zA-Z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex SpecialCharRegex = new(@"[^a-zA-Z0-9\s]", RegexOptions.Compiled);

    public string Hash { get; }

    private Password(string hash)
    {
        Hash = hash;
    }

    public static Password FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ValidationException(nameof(Password), "O hash da senha não pode ser vazio.");

        return new Password(hash);
    }

    public static void ValidatePlainPassword(string? plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ValidationException(nameof(Password), "A senha não pode ser vazia.");

        if (plainPassword.Length < 8 || plainPassword.Length > 20)
            throw new ValidationException(nameof(Password), "A senha deve ter entre 8 e 20 caracteres.");

        if (!LetterRegex.IsMatch(plainPassword))
            throw new ValidationException(nameof(Password), "A senha deve conter pelo menos uma letra.");

        if (!DigitRegex.IsMatch(plainPassword))
            throw new ValidationException(nameof(Password), "A senha deve conter pelo menos um número.");

        if (!SpecialCharRegex.IsMatch(plainPassword))
            throw new ValidationException(nameof(Password), "A senha deve conter pelo menos um caractere especial.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }

    public override string ToString() => "[PROTECTED]";
}

