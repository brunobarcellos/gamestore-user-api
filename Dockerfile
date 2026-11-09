# ==============================================================================
# Stage 1: Build & Publish (Multi-Stage Build para Otimização em Produção)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto (.csproj) separadamente para aproveitar o cache de camadas do Docker
COPY ["src/GameStore.Users.Domain/GameStore.Users.Domain.csproj", "src/GameStore.Users.Domain/"]
COPY ["src/GameStore.Users.Application/GameStore.Users.Application.csproj", "src/GameStore.Users.Application/"]
COPY ["src/GameStore.Users.Infrastructure/GameStore.Users.Infrastructure.csproj", "src/GameStore.Users.Infrastructure/"]
COPY ["src/GameStore.Users.Api/GameStore.Users.Api.csproj", "src/GameStore.Users.Api/"]

RUN dotnet restore "src/GameStore.Users.Api/GameStore.Users.Api.csproj"

# Copia o restante do código-fonte e publica em modo Release
COPY src/ src/
WORKDIR "/src/src/GameStore.Users.Api"
RUN dotnet publish "GameStore.Users.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Stage 2: Runtime enxuto (ASP.NET Core Runtime)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Cria diretório para persistência do banco SQLite com permissões para usuário não-root
RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app/data

USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GameStore.Users.Api.dll"]

