using GameStore.Users.Application.Common.Interfaces;
using GameStore.Users.Domain.Entities;
using GameStore.Users.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameStore.Users.Infrastructure.Persistence.Seeding;

public static class UsersDbInitializer
{
    public static readonly Guid DefaultAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultGamerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task InitializeAsync(
        UsersDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            if (await context.Users.AnyAsync())
            {
                return;
            }

            logger.LogInformation("Iniciando o seeding inicial do banco de dados da UserAPI...");

            var adminEmail = Email.Create("admin@gamestore.com");
            var adminPasswordHash = passwordHasher.HashPassword("Admin@123456");
            var admin = User.CreateAdmin("Administrador Main", adminEmail, Password.FromHash(adminPasswordHash), DefaultAdminId);

            var userEmail = Email.Create("jogador@gamestore.com");
            var userPasswordHash = passwordHasher.HashPassword("Gamer@123456");
            var standardUser = new User("Jogador Alpha", userEmail, Password.FromHash(userPasswordHash), id: DefaultGamerId);

            await context.Users.AddRangeAsync(admin, standardUser);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeding da UserAPI concluído com sucesso! Usuários criados: admin@gamestore.com e jogador@gamestore.com.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro durante a inicialização/seeding do banco de dados da UserAPI.");
            throw;
        }
    }
}

