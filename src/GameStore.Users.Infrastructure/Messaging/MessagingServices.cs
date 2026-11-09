using GameStore.Users.Application.Common.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GameStore.Users.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public QueueNamesOptions Queues { get; set; } = new();
}

public class QueueNamesOptions
{
    public string UserCreated { get; set; } = "UserCreatedEvent";
}

public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitEventPublisher> _logger;

    public MassTransitEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        await _publishEndpoint.Publish(message, cancellationToken);
        _logger.LogInformation("Mensagem {EventType} publicada no RabbitMQ com sucesso.", typeof(T).Name);
    }
}

