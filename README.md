# HappyCRM — Internal Integration Dashboard

> A production-grade internal dashboard for monitoring customer integrations, tracking job executions, and managing CRM data in real time.

**Live Demo:** [https://happy-crm-gamma.vercel.app](https://happy-crm-gamma.vercel.app)  
**Backend API:** [https://happycrm-production.up.railway.app](https://happycrm-production.up.railway.app)

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Workflow](#workflow)
- [Project Structure](#project-structure)
- [Getting Started (Local Dev)](#getting-started-local-dev)
- [Deployment](#deployment)
- [API Reference](#api-reference)
- [Using the Live Project](#using-the-live-project)
- [Future Roadmap](#future-roadmap)

---

## Overview

HappyCRM is an internal operations dashboard designed to give teams full visibility over their customer integrations. It surfaces real-time execution metrics, job success/failure rates, and customer data — all in one place.

Key capabilities:
- 📊 **Dashboard** — live stats cards (total jobs, success rate, failures, pending)
- 🔁 **Integration Jobs** — view all integration jobs, retry failed ones instantly
- ⚡ **Executions** — paginated log of every job execution with status and timestamps
- 👥 **Customers** — full CRUD: create, view, and edit customer records

---

## Tech Stack

### Backend
| Layer | Technology |
|---|---|
| Runtime | .NET 10 (ASP.NET Core Web API) |
| ORM | Entity Framework Core 10 |
| Database | Neon PostgreSQL (serverless) |
| Architecture | Clean Architecture — API / Core / Infrastructure |
| Testing | NUnit + NSubstitute |
| Containerisation | Docker (multi-stage build) |
| Hosting | Railway |

### Frontend
| Layer | Technology |
|---|---|
| Framework | React 19 + Vite 7 |
| Styling | Tailwind CSS v4 |
| HTTP | Fetch API (custom thin client) |
| Hosting | Vercel |

### DevOps
| Tool | Purpose |
|---|---|
| GitHub Actions | CI — build + test on every push |
| Docker | Reproducible backend builds |
| Railway | Backend container hosting + env var management |
| Vercel | Frontend CDN + automatic preview deployments |

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                     Browser                         │
│           React + Vite (Vercel CDN)                 │
└──────────────────────┬──────────────────────────────┘
                       │ HTTPS (CORS-controlled)
┌──────────────────────▼──────────────────────────────┐
│              ASP.NET Core Web API                   │
│                  (Railway)                          │
│  ┌────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ Controllers│  │  Services   │  │  Middleware  │  │
│  └─────┬──────┘  └──────┬──────┘  └─────────────┘  │
│        └────────────────┘                           │
│              EF Core (Npgsql)                       │
└──────────────────────┬──────────────────────────────┘
                       │ SSL
┌──────────────────────▼──────────────────────────────┐
│           Neon PostgreSQL (serverless)              │
└─────────────────────────────────────────────────────┘
```

The backend follows **Clean Architecture**:
- **`InternalDashboard.API`** — controllers, middleware, DI wiring, `Program.cs`
- **`InternalDashboard.Core`** — domain models, DTOs, service interfaces (zero dependencies)
- **`InternalDashboard.Infrastructure`** — EF Core `AppDbContext`, migrations, service implementations, database seeder

---

## Workflow

### How a request flows end to end

1. **User opens the dashboard** at the Vercel URL
2. **React** renders the UI and fires `fetch()` calls to the Railway backend via `VITE_API_URL`
3. **ASP.NET Core** receives the request, validates CORS (only the Vercel origin is allowed), and routes it to the correct controller
4. **The controller** calls a service interface (e.g. `IDashboardService`)
5. **The service** queries Neon PostgreSQL via EF Core and returns typed DTOs
6. **JSON** is serialised and returned to the frontend
7. **React** updates the UI with live data

### Job Retry Flow

1. User clicks **Retry** on a failed job in the Integration Jobs page
2. `POST /api/jobs/{id}/retry` is called
3. The backend resets the job status to `Pending` and increments the retry counter
4. The job reappears in the pending queue

---

## Project Structure

```
/
├── backend/                                  # ASP.NET Core solution → Railway
│   ├── Dockerfile                            # Multi-stage build (sdk:10.0 → aspnet:10.0)
│   ├── railway.json                          # Railway deployment config
│   ├── src/
│   │   ├── InternalDashboard.API/            # Entry point — controllers, middleware, Program.cs
│   │   ├── InternalDashboard.Core/           # Models, DTOs, service interfaces
│   │   └── InternalDashboard.Infrastructure/ # EF Core, migrations, services, seeder
│   └── tests/
│       └── InternalDashboard.Tests/          # NUnit unit tests
│
├── frontend/                                 # React + Vite → Vercel
│   ├── vercel.json                           # SPA rewrites + build config
│   ├── vite.config.js                        # Dev proxy → localhost:5050 | prod build
│   ├── .env.production                       # VITE_API_URL → Railway backend
│   └── src/
│       ├── api/                              # client.js, customersApi, jobsApi, statsApi
│       ├── components/                       # Reusable UI components
│       └── pages/                            # Dashboard, Customers, IntegrationJobs, Execution
│
└── .github/workflows/ci.yml                  # CI: build + test backend, build frontend
```

---

## Getting Started (Local Dev)

### Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0+ |
| Node.js | 20+ |
| `dotnet-ef` CLI | `dotnet tool install -g dotnet-ef` |

### 1. Clone

```bash
git clone https://github.com/Dhruv7-code/HappyCRM.git
cd HappyCRM
```

### 2. Backend

```bash
cd backend/src/InternalDashboard.API

# Set your Neon connection string via user secrets (never commit credentials)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=<neon-host>;Database=<db>;Username=<user>;Password=<pw>;SSL Mode=Require;Trust Server Certificate=true"

# Run — starts on http://localhost:5050
dotnet run
```

### 3. Frontend

```bash
cd frontend

# .env is gitignored — create from example
cp .env.example .env
# VITE_API_URL=http://localhost:5050 is already set

npm install
npm run dev   # starts on http://localhost:5173
```

The Vite dev server proxies `/api/*` → `http://localhost:5050` automatically — no CORS configuration needed locally.

---

## Deployment

### Backend → Railway

| Environment Variable | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | Neon connection string (key-value or `postgresql://` URI — both formats work) |
| `AllowedCorsOrigins__0` | Your Vercel frontend URL (e.g. `https://happy-crm-gamma.vercel.app`) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

- Set **Root Directory** to `backend` in the Railway dashboard
- Railway picks up `railway.json` and builds using the `Dockerfile` automatically

### Frontend → Vercel

- Set **Root Directory** to `frontend` in the Vercel dashboard
- No extra env vars needed — `VITE_API_URL` is committed in `frontend/.env.production`
- Vercel handles SPA routing automatically via `vercel.json` rewrites

---

## API Reference

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/stats` | Aggregate stats (totals, success rate) |
| `GET` | `/api/stats/jobs/success` | Count of successful jobs |
| `GET` | `/api/stats/jobs/failed` | Count of failed jobs |
| `GET` | `/api/stats/jobs/pending` | Count of pending jobs |
| `GET` | `/api/stats/customers/total` | Total customer count |
| `GET` | `/api/customers` | List all customers |
| `POST` | `/api/customers/newcustomer` | Create a new customer |
| `PUT` | `/api/customers/{id}/updateuser` | Update a customer |
| `GET` | `/api/dashboard/jobs` | All integration jobs |
| `GET` | `/api/dashboard/executions` | All job executions |
| `GET` | `/api/dashboard/recent-executions` | Most recent executions |
| `POST` | `/api/jobs/{id}/retry` | Retry a failed job |

---

## Using the Live Project

1. Open **[https://happy-crm-gamma.vercel.app](https://happy-crm-gamma.vercel.app)** in your browser — no login required
2. Navigate using the **sidebar**:

| Page | What you can do |
|---|---|
| **Dashboard** | See live KPIs — total jobs, success rate, failure count, recent activity |
| **Integration Jobs** | Browse all jobs, filter by status, retry any failed job with one click |
| **Executions** | Full paginated log of every execution with status and timestamps |
| **Customers** | Browse all customers, add a new customer, or edit an existing record |

3. All data is live from the production Neon database — changes persist immediately

---

## Future Roadmap

### 🔐 Authentication & Role-Based Access
Integrate ASP.NET Core Identity or a third-party provider (Auth0 / Entra ID) to protect the dashboard behind a login screen. Role definitions (Admin, Operator, Viewer) would gate who can trigger retries, create customers, or only view data.

### 📋 Full Job Execution Log Viewer
Each execution currently shows a summary status. A future version would expose a detailed scrollable log viewer — surfacing full stdout/stderr output line by line so operators can diagnose root causes without leaving the dashboard.

### 🔔 Failure Alerts & Notifications
Webhook or email notifications (via SendGrid or a Slack bot) triggered automatically when a job permanently fails or exceeds its retry threshold — so teams are alerted before users notice.

### 📈 Historical Analytics & Trend Charts
Time-series charts (Recharts or Chart.js) showing job success/failure trends over configurable time windows — giving operations teams a clear picture of system health over days and weeks.

### 🌐 Multi-Tenant Support
Namespace jobs and customers by organisation so the same dashboard instance can serve multiple teams with full data isolation, without spinning up separate deployments.

---

## CI Pipeline

On every push to `master`, GitHub Actions:
1. Restores and builds the backend solution (`dotnet build -c Release`)
2. Runs all NUnit tests (`dotnet test`)
3. Installs frontend dependencies and runs `npm run build`

---

*Built with .NET 10, React 19, and deployed on Railway + Vercel.*
