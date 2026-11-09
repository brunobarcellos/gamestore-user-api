using FluentValidation;
using GameStore.Users.Application.Interfaces;
using GameStore.Users.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.Users.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}

