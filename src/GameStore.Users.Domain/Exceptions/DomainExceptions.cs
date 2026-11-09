namespace GameStore.Users.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { { "General", [message] } };
    }

    public ValidationException(string propertyName, string errorMessage) : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]> { { propertyName, [errorMessage] } };
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Ocorreram um ou mais erros de validação.")
    {
        Errors = errors;
    }
}

public class NotFoundException : DomainException
{
    public string EntityName { get; }
    public object Key { get; }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} com identificador '{key}' não foi encontrado(a).")
    {
        EntityName = entityName;
        Key = key;
    }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

public class EmailAlreadyInUseException : ConflictException
{
    public EmailAlreadyInUseException(string email)
        : base($"O e-mail '{email}' já está cadastrado no sistema.") { }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Credenciais inválidas ou acesso não autorizado.")
        : base(message) { }
}

