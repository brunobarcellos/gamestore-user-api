# GameStore - UserAPI

Microsserviço responsável pelo **cadastro de usuários**, **autenticação via geração de token JWT**, **autorização (RBAC)** e publicação do evento `UserCreatedEvent` na fila RabbitMQ.

## Executar Localmente (.NET 10)
```bash
dotnet run --project src/GameStore.Users.Api
```

## Build Docker (Multi-Stage)
```bash
docker build -t gamestore/user-api:latest .
```

## Deploy Kubernetes
```bash
kubectl apply -f k8s/
```

## Executar Testes
```bash
dotnet test UserAPI.slnx
```

