using System.Text.RegularExpressions;
using FluentValidation;
using GameStore.Users.Application.DTOs;

namespace GameStore.Users.Application.Validators;

public class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    private static readonly Regex LetterRegex = new(@"[a-zA-Z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex SpecialCharRegex = new(@"[^a-zA-Z0-9\s]", RegexOptions.Compiled);

    public RegisterUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .Length(2, 100).WithMessage("O nome deve ter entre 2 e 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("O formato do e-mail é inválido.")
            .MaximumLength(254).WithMessage("O e-mail não pode ter mais de 254 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve conter no mínimo 8 caracteres.")
            .Must(p => LetterRegex.IsMatch(p ?? string.Empty)).WithMessage("A senha deve conter pelo menos uma letra.")
            .Must(p => DigitRegex.IsMatch(p ?? string.Empty)).WithMessage("A senha deve conter pelo menos um número.")
            .Must(p => SpecialCharRegex.IsMatch(p ?? string.Empty)).WithMessage("A senha deve conter pelo menos um caractere especial (ex: @, #, $, %, &, *).");
    }
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("O formato do e-mail é inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.");
    }
}

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .Length(2, 100).WithMessage("O nome deve ter entre 2 e 100 caracteres.");
    }
}

