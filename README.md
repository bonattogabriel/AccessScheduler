# Agendador de Acessos

Sistema de agendamento de acessos com controle de concorrência otimista, desenvolvido em .NET 8 com ASP.NET Core Minimal API e Blazor Server.
### Tecnologias Utilizadas
- **.NET 8**
- **ASP.NET Core Minimal API**
- **Blazor Server**
- **Entity Framework Core**
- **SQL Server**
- **xUnit**
- **Docker**

#Executando com Docker

###Pré-requisitos

- **Docker Desktop instalado e executando**
- **Docker Compose (geralmente incluído no Docker Desktop)**

###1. Clone o repositório
```
git clone <repository-url>
cd AccessScheduler
```

###2. Execute com Docker Compose
```
docker-compose up --build -d
```

###3. Acesse as aplicações

-**API: http://localhost:7293/swagger **
-**Blazor App: http://localhost:7059 **