using GameStore.Users.Application.DTOs;
using GameStore.Users.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Users.Api.Endpoints;

public static class UserAndAuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("1. Autenticação e Registro");

        group.MapPost("/register", async (
            [FromBody] RegisterUserRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var response = await authService.RegisterAsync(request, ct);
            return Results.Created($"/api/users/{response.User.Id}", response);
        })
        .WithName("RegisterUser")
        .WithSummary("Cadastrar um novo usuário e publicar UserCreatedEvent")
        .Produces<AuthResponse>(StatusCodes.Status201Created)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var response = await authService.LoginAsync(request, ct);
            return Results.Ok(response);
        })
        .WithName("LoginUser")
        .WithSummary("Autenticar usuário e obter token JWT")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("2. Gestão de Usuários");

        group.MapGet("/me", async (
            IUserService userService,
            CancellationToken ct) =>
        {
            var user = await userService.GetCurrentUserProfileAsync(ct);
            return Results.Ok(user);
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Obter perfil do usuário autenticado")
        .Produces<UserResponse>(StatusCodes.Status200OK);

        group.MapGet("/", async (
            IUserService userService,
            CancellationToken ct) =>
        {
            var users = await userService.GetAllAsync(ct);
            return Results.Ok(users);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("GetAllUsers")
        .WithSummary("Listar todos os usuários (Apenas Administrador)")
        .Produces<IEnumerable<UserResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (
            Guid id,
            IUserService userService,
            CancellationToken ct) =>
        {
            var user = await userService.GetByIdAsync(id, ct);
            return Results.Ok(user);
        })
        .AllowAnonymous() // Permite consulta interna entre microsserviços no cluster (ex.: CatalogAPI / NotificationsAPI)
        .WithName("GetUserById")
        .WithSummary("Obter detalhes de um usuário por ID")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequest request,
            IUserService userService,
            CancellationToken ct) =>
        {
            var updatedUser = await userService.UpdateProfileAsync(id, request, ct);
            return Results.Ok(updatedUser);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdateUser")
        .WithSummary("Atualizar informações de um usuário (Apenas Administrador)")
        .Produces<UserResponse>(StatusCodes.Status200OK);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IUserService userService,
            CancellationToken ct) =>
        {
            await userService.DeactivateAsync(id, ct);
            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly")
        .WithName("DeactivateUser")
        .WithSummary("Desativar usuário (Apenas Administrador)")
        .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            IUserService userService,
            CancellationToken ct) =>
        {
            await userService.ActivateAsync(id, ct);
            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly")
        .WithName("ActivateUser")
        .WithSummary("Reativar usuário (Apenas Administrador)")
        .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

