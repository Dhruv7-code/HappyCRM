# Internal Dashboard

An internal organisation dashboard built with **ASP.NET Core (.NET 10)** on the backend and **React + Tailwind CSS** on the frontend, backed by **Neon PostgreSQL** via Entity Framework Core.

---

## Project structure

```
/
├── src/
│   ├── InternalDashboard.API/           # ASP.NET Core Web API (entry point)
│   ├── InternalDashboard.Core/          # Domain models, interfaces, DTOs
│   └── InternalDashboard.Infrastructure/# EF Core DbContext, repositories
├── tests/
│   └── InternalDashboard.Tests/         # NUnit unit & integration tests
├── frontend/                            # React + Vite + Tailwind CSS
└── .github/workflows/ci.yml             # GitHub Actions CI pipeline
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0+ |
| Node.js | 20+ |
| npm | 10+ |
| `dotnet-ef` CLI | 10.0+ (`dotnet tool install -g dotnet-ef`) |

---

## Getting started

### 1. Configure the database connection

Use .NET User Secrets (never commit real credentials):

```bash
cd src/InternalDashboard.API

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=<your-neon-host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
```

> You can find your Neon connection string in the **Neon Console → Project → Connection Details**.

### 2. Run database migrations

```bash
# Create a migration (once you have entities in Core)
dotnet ef migrations add InitialCreate \
  --project src/InternalDashboard.Infrastructure \
  --startup-project src/InternalDashboard.API

# Apply to the database
dotnet ef database update \
  --project src/InternalDashboard.Infrastructure \
  --startup-project src/InternalDashboard.API
```

### 3. Start the API

```bash
dotnet run --project src/InternalDashboard.API
# API available at http://localhost:5000
# OpenAPI docs at http://localhost:5000/openapi/v1.json
```

### 4. Start the frontend

```bash
cd frontend
cp .env.example .env.local   # fill in VITE_API_BASE_URL if needed
npm run dev
# App available at http://localhost:5173
```

---

## Running tests

```bash
# All tests
dotnet test InternalDashboard.slnx --verbosity normal

# With code coverage
dotnet test InternalDashboard.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

---

## CI/CD (GitHub Actions)

The pipeline runs on every push and pull request to `main` / `develop`:

| Job | Steps |
|-----|-------|
| **backend** | restore → build → NUnit tests + coverage |
| **frontend** | install → lint → Vite build |

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `NEON_CONNECTION_STRING` | Full Neon PostgreSQL connection string for CI |
| `VITE_API_BASE_URL` | API base URL used during the frontend build |

---

## Tech stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10, EF Core 10, Npgsql |
| Database | Neon PostgreSQL (serverless) |
| Frontend | React 19, Vite 7, Tailwind CSS 4 |
| Testing | NUnit 4, Coverlet, MVC.Testing |
| CI | GitHub Actions |
