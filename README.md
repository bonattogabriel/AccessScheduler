# AccessScheduler

Sistema de agendamento de acessos com controle de concorrência, captura de localização e upload de fotos.

## Arquitetura

## Tecnologias Utilizadas

### Backend
- **ASP.NET Core 8**: Framework web
- **Entity Framework Core**: ORM
- **SQL Server 2022**: Banco de dados
- **FluentValidation**: Validação de dados
- **Serilog**: Logging estruturado

### Frontend
- **Blazor Server**: Framework UI
- **Bootstrap 5**: Framework CSS
- **JavaScript**: Interop para geolocalização

### DevOps
- **Docker**: Containerização
- **Docker Compose**: Orquestração local

### Testes
- **xUnit**: Framework de testes
- **Moq**: Mocking
- **Microsoft.AspNetCore.Mvc.Testing**: Testes de integração

### Padrões Arquiteturais Utilizados

#### Repository Pattern
```csharp
public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(BookingRequest request, string timeZoneId);
    Task<bool> CancelBookingAsync(Guid bookingId);
    Task<List<FreeSlot>> GetFreeSlotsAsync(DateTime date, int durationMinutes, string resource, string timeZoneId);
    Task<List<TimeSlot>> GetAlternativeSlotsAsync(string resource, DateTime startUtc, DateTime endUtc);
}
```

#### DTO (Data Transfer Objects)
- Separação clara entre modelos de domínio e contratos de API
- Validação centralizada usando Data Annotations

#### Dependency Injection
- Configuração de serviços no Program.cs
- Facilita testes unitários e manutenibilidade

---

## Como Rodar Local

### Pré-requisitos

- .NET 8 SDK
- Docker Desktop
- Visual Studio 2022 ou VS Code (opcional)

### Opção 1: Docker Compose (Recomendado)

```bash
# 1. Clone o repositório
git clone https://github.com/bonattogabriel/AccessScheduler.git
cd AccessScheduler

# 2. Execute com Docker Compose
docker-compose up -d

# 3. Acesse a aplicação
# - Blazor App: http://localhost:7059
# - API: http://localhost:7293
```

### Opção 2: Desenvolvimento Local

```bash
# 1. Configure o SQL Server
# - Instale SQL Server LocalDB ou use Docker para SQL Server

# 2. Configure connection string
# Edite appsettings.json nos projetos API e Blazor

# 3. Execute as migrações
cd AccessScheduler.API
dotnet ef database update

# 4. Execute a API
dotnet run --project AccessScheduler.API

# 5. Execute o Blazor (em outro terminal)
dotnet run --project AccessScheduler
```

### Configuração de Ambiente

#### appsettings.json (API)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AccessSchedulerDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### appsettings.json (Blazor)
```json
{
  "ApiBaseUrl": "https://localhost:7293/",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### 3. Regras de Negócio

**Horário de Funcionamento**
- Segunda à Sexta: 08:00 às 18:00
- Sábados: 08:00 às 12:00
- Domingos: Fechado

**Duração dos Slots**
- Padrão: 30 minutos
- Mínimo: 15 minutos
- Máximo: 2 horas

**Recursos Disponíveis**
- gate-1: Portão Principal
- gate-2: Portão Secundário
- gate-3: Portão de Serviço
