using GameStore.Users.Application.Common.Interfaces;
using GameStore.Users.Domain.Repositories;
using GameStore.Users.Infrastructure.Messaging;
using GameStore.Users.Infrastructure.Persistence;
using GameStore.Users.Infrastructure.Persistence.Repositories;
using GameStore.Users.Infrastructure.Security;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=users.db";
        services.AddDbContext<UsersDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UsersDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        var rabbitOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitOptions.Host, rabbitOptions.Port, rabbitOptions.VirtualHost, h =>
                {
                    h.Username(rabbitOptions.Username);
                    h.Password(rabbitOptions.Password);
                });
            });
        });

        return services;
    }
}

