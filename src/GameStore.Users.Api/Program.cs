using GameStore.Users.Api.Endpoints;
using GameStore.Users.Api.Infrastructure;
using GameStore.Users.Application;
using GameStore.Users.Application.Common.Interfaces;
using GameStore.Users.Infrastructure;
using GameStore.Users.Infrastructure.Persistence;
using GameStore.Users.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GameStore UserAPI v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "GameStore UserAPI - Swagger UI";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health").ExcludeFromDescription();

app.MapAuthEndpoints();
app.MapUserEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<UsersDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher>();

    await UsersDbInitializer.InitializeAsync(dbContext, passwordHasher, logger);
}

app.Run();

public partial class Program { }

