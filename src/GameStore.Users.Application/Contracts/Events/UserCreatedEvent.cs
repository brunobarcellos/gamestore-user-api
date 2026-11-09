namespace GameStore.Contracts.Events;

/// <summary>
/// Evento de integração publicado na fila RabbitMQ quando um novo usuário é cadastrado no UserAPI.
/// Consumido pelo NotificationsAPI para envio de e-mail de boas-vindas.
/// </summary>
public record UserCreatedEvent(
    Guid UserId,
    string Name,
    string Email,
    DateTime CreatedAt
);

