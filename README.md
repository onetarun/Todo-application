# Todo Application

[![CI](https://github.com/onetarun/Todo-application/actions/workflows/ci.yml/badge.svg)](https://github.com/onetarun/Todo-application/actions/workflows/ci.yml)

A full-stack todo app. Registered users organize tasks into lists; each task has a status, priority, due date,
subtasks, notes, reminders and tags. Reminders pop up as in-app notifications while the app is open.

| Part | Technology |
|------|------------|
| API | ASP.NET Core Web API (.NET 10, controllers), EF Core 10, SQL Server (LocalDB in development) |
| Auth | JWT access tokens (15 min) + rotating refresh tokens (7 days, stored hashed) |
| Client | Angular 22 (standalone components, signals), Angular Material 22, Reactive Forms |
| Tests | xUnit (API), Vitest via `ng test` (client), Postman collection (API contract) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (`MSSQLLocalDB`, installed with Visual Studio or the SQL Server Express installer) — Windows
- [Node.js](https://nodejs.org/) 22.22+ or 24.15+ (npm included)
- EF Core tools: `dotnet tool install --global dotnet-ef`
- Optional: [Postman CLI](https://learning.postman.com/docs/postman-cli/postman-cli-overview/) to run the API collection

## One-time setup

```bash
# JWT signing key: kept in user secrets, never in source control (at least 32 bytes)
dotnet user-secrets set "Jwt:SigningKey" "<random-base64-64-bytes>" --project src/TodoApi
#   PowerShell, to generate one: [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))

# Create the LocalDB database "TodoApp" and apply the migrations (seeds demo data in Development)
dotnet ef database update --project src/TodoApi

# Client dependencies
cd client
npm install
```

## Run

```bash
# API — http://localhost:5055 (OpenAPI document at /openapi/v1.json in Development)
dotnet run --project src/TodoApi

# Client — http://localhost:4200 (in another terminal; /api is proxied to the API by client/proxy.conf.json)
cd client
npm start
```

Sign in with the demo account `demo@todo.local` / `Demo123!`, or create a new account.

## Test

```bash
dotnet test TodoApplication.slnx      # API: service + endpoint tests on a throwaway LocalDB database (dropped afterwards)

cd client
npm test -- --watch=false             # client: services, interceptor, guards, notifier and pages

# API contract (with the API running): registers a fresh user, exercises every endpoint, cleans up after itself
postman collection run postman/TodoApi.postman_collection.json -e postman/TodoApi.local.postman_environment.json
```

CI (`.github/workflows/ci.yml`) runs the API build and tests on Windows (LocalDB) and the client build and tests on
Linux for every push and pull request.

## API overview

All routes are under `/api` and require a bearer token except register, login, refresh and logout. Errors are
returned as RFC 7807 `application/problem+json`. Another user's data always answers `404`.

| Area | Endpoints |
|------|-----------|
| Auth | `POST auth/register` · `POST auth/login` · `POST auth/refresh` · `POST auth/logout` · `GET auth/me` |
| Lists | `GET/POST lists` · `GET/PUT/DELETE lists/{id}` |
| Tasks | `GET/POST lists/{listId}/tasks` (filters: `status`, `priority`, `tagId`) · `GET/PUT/DELETE tasks/{id}` · `PATCH tasks/{id}/status` |
| Subtasks | `GET/POST tasks/{taskId}/subtasks` · `PUT/DELETE subtasks/{id}` · `PATCH subtasks/{id}/complete` |
| Notes | `GET/POST tasks/{taskId}/notes` · `PUT/DELETE notes/{id}` |
| Reminders | `GET/POST tasks/{taskId}/reminders` · `DELETE reminders/{id}` · `GET reminders?before={utc}` (due in the last 24 h) |
| Tags | `GET/POST tags` · `PUT/DELETE tags/{id}` · `PUT/DELETE tasks/{taskId}/tags/{tagId}` |

The full contract, with examples and checks, is in the Postman collection; the detailed plan and request shapes are
in [docs/plan.md](docs/plan.md).

## Project layout

```
Todo-application/
├── src/TodoApi/            # Web API: Controllers, Services, Dtos, Entities, Data (DbContext, configurations,
│                           #   seeder), Auth (JWT), Migrations
├── tests/TodoApi.Tests/    # xUnit: service tests and endpoint tests (WebApplicationFactory)
├── client/                 # Angular app
│   └── src/app/            #   core (auth, interceptor, guards, reminder notifier), models, services,
│                           #   features (auth, lists, tasks, tags), shared
├── postman/                # API collection + local environment
├── docs/plan.md            # implementation plan and decisions
├── PROGRESS.md             # progress tracking
└── .github/workflows/      # CI
```

## Design notes

- **Tokens:** the access token lives only in memory; the refresh token is kept in `localStorage` so a reload keeps
  you signed in (script-readable — a deliberate trade-off, mitigated by short-lived access tokens and refresh-token
  rotation with reuse detection). Reusing an old refresh token signs out every session of that user.
- **Dates:** the API stores and returns UTC (ISO 8601 with `Z`). A due date is a calendar day (UTC midnight, shown
  the same in every time zone); a reminder is an instant, entered and shown in local time.
- **Tags are global:** every user sees the same tags, and renaming or deleting one affects every task that uses it.
- **Reminders** are in-app only: the client checks every minute while you are signed in and shows each due reminder
  once (remembered per browser). Nothing is sent when the app is closed, and reminders missed by more than 24 hours
  are not shown.
- **Theme:** follows the operating system's light/dark setting; Material animations are off when the OS asks for
  reduced motion.
