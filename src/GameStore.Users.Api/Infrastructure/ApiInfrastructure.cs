using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GameStore.Users.Application.Common.Interfaces;
using GameStore.Users.Domain.Enums;
using GameStore.Users.Domain.Exceptions;
using GameStore.Users.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace GameStore.Users.Api.Infrastructure;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? UserEmail =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public string? UserRole =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("role");

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

public static class ApiExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToString()))
            .AddPolicy("UserOrAdmin", policy => policy.RequireRole(UserRole.User.ToString(), UserRole.Admin.ToString()));

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "GameStore - UserAPI",
                Version = "v1",
                Description = "Microsserviço de Cadastro, Autenticação JWT e Gestão de Usuários (publica UserCreatedEvent)."
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Autenticação JWT no cabeçalho. Exemplo: 'Bearer {seu_token}'",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);
            c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            });
        });

        return services;
    }
}

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ValidationProblemDetails(validationEx.Errors.ToDictionary(k => k.Key, v => v.Value))
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Erro de validação de dados.",
                    Detail = validationEx.Message,
                    Instance = context.Request.Path
                }
            ),
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ProblemDetails
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = "Recurso não encontrado.",
                    Detail = notFoundEx.Message,
                    Instance = context.Request.Path
                }
            ),
            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Conflict,
                    Title = "Conflito de negócio.",
                    Detail = conflictEx.Message,
                    Instance = context.Request.Path
                }
            ),
            UnauthorizedException unauthEx => (
                HttpStatusCode.Unauthorized,
                new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = "Não autorizado.",
                    Detail = unauthEx.Message,
                    Instance = context.Request.Path
                }
            ),
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Violação de regra de negócio.",
                    Detail = domainEx.Message,
                    Instance = context.Request.Path
                }
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Ocorreu um erro interno no servidor.",
                    Detail = "Ocorreu uma falha inesperada ao processar sua solicitação.",
                    Instance = context.Request.Path
                }
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "[TraceId: {TraceId}] Erro não tratado em {Method} {Path}", traceId, context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("[TraceId: {TraceId}] Erro controlado ({StatusCode}): {Message}", traceId, (int)statusCode, exception.Message);

        problemDetails.Extensions["traceId"] = traceId;
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize((object)problemDetails, options));
    }
}

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "[UserAPI] HTTP {Method} {Path} -> {StatusCode} ({ElapsedMs}ms)",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}

