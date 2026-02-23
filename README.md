# Internal Dashboard

An internal organisation dashboard built with **ASP.NET Core (.NET 10)** on the backend and **React + Tailwind CSS** on the frontend, backed by **Neon PostgreSQL** via Entity Framework Core.

---

## Project structure

```
/
├── backend/                             # ASP.NET Core solution (deployed to Railway)
│   ├── src/
│   │   ├── InternalDashboard.API/       # Web API entry point
│   │   ├── InternalDashboard.Core/      # Domain models, interfaces, DTOs
│   │   └── InternalDashboard.Infrastructure/ # EF Core DbContext, migrations
│   ├── tests/
│   │   └── InternalDashboard.Tests/     # NUnit unit & integration tests
│   └── InternalDashboard.slnx           # Solution file
├── frontend/                            # React + Vite + Tailwind CSS (deployed to Vercel)
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

### Backend

#### 1. Configure the database connection

Use .NET User Secrets (never commit real credentials):

```bash
cd backend/src/InternalDashboard.API

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=<your-neon-host>;Port=5432;Database=<db>;Username=<user>;Password=<pw>;SSL Mode=Require;Trust Server Certificate=true"
```

> You can find your Neon connection string in the **Neon Console → Project → Connection Details**.

> **SSL note:** `Trust Server Certificate=true` is required locally because Neon's connection pooler
> certificate may not match the hostname on your machine. In **production** (Railway) set
> `Trust Server Certificate=false` — the Railway environment has a proper CA trust store.

#### 2. Run database migrations

```bash
cd backend

dotnet ef migrations add InitialCreate \
  --project src/InternalDashboard.Infrastructure \
  --startup-project src/InternalDashboard.API

dotnet ef database update \
  --project src/InternalDashboard.Infrastructure \
  --startup-project src/InternalDashboard.API
```

#### 3. Start the API

```bash
cd backend
dotnet run --project src/InternalDashboard.API
# API available at http://localhost:5050
# OpenAPI docs at http://localhost:5050/openapi/v1.json (Development only)
```

### Frontend

#### 4. Configure and start

```bash
cd frontend
cp .env.example .env        # already contains VITE_API_URL=http://localhost:5050
npm install
npm run dev
# App available at http://localhost:5173
```

`VITE_API_URL` controls where the frontend sends API requests. Set it to your
Railway backend URL when deploying to Vercel.

---

## Running tests

```bash
cd backend
dotnet test InternalDashboard.slnx --verbosity normal

# With code coverage
dotnet test InternalDashboard.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

---

## Deployment

### Backend → Railway

Set these environment variables in the Railway service → **Variables** panel:

| Variable | Value |
|----------|-------|
| `ConnectionStrings__DefaultConnection` | `Host=...;SSL Mode=Require;Trust Server Certificate=false` |
| `AllowedCorsOrigins__0` | `https://<your-vercel-frontend-url>` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

Railway **root directory** setting: `backend`
Railway **start command**: `dotnet src/InternalDashboard.API/bin/Release/net10.0/InternalDashboard.API.dll`

### Frontend → Vercel

Set this environment variable in Vercel → **Settings → Environment Variables**:

| Variable | Value |
|----------|-------|
| `VITE_API_URL` | `https://<your-railway-backend-url>` |

Vercel **root directory** setting: `frontend`

---

## CI/CD (GitHub Actions)

The pipeline runs on every push and pull request to `main` / `develop`:

| Job | Steps |
|-----|-------|
| **backend** | restore → build (Release) → NUnit tests + coverage |
| **frontend** | npm ci → lint → Vite build |

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `NEON_CONNECTION_STRING` | Full Neon PostgreSQL connection string for CI tests |
| `VITE_API_URL` | Production API base URL used during the frontend Vite build |

---

## Tech stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10, EF Core 10, Npgsql |
| Database | Neon PostgreSQL (serverless) |
| Frontend | React 19, Vite 7, Tailwind CSS 4 |
| Testing | NUnit 4, Coverlet |
| CI | GitHub Actions |
| Backend hosting | Railway |
| Frontend hosting | Vercel |
