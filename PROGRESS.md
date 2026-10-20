# Progress

Detailed plan: [docs/plan.md](docs/plan.md)

| Feature | Status | Date | Description | Areas |
|---------|--------|------|-------------|-------|
| Phase 0 — Project setup | Done | 2026-10-20 | Solution, Angular client, CLAUDE.md, git | API, client |
| Phase 1 — Backend foundation | Done | 2026-10-20 | EF Core + SQL Server (LocalDB), 8 entities with configuration, `InitialCreate` migration, idempotent dev seed, CORS for `http://localhost:4200`, ProblemDetails error handling; WeatherForecast sample removed | API, database |
| Phase 2 — Authentication | Done | 2026-10-20 | Custom JWT (15 min) + rotating refresh tokens (7 days, stored hashed, reuse revokes all); `/api/auth` register, login, refresh, logout, me; all endpoints require auth by default; `AddRefreshTokens` migration | API, database |
| Phase 3 — Lists & Tasks API | Done | 2026-10-20 | `/api/lists` and `/api/tasks` CRUD, task filters (status, priority, tag), status PATCH; every query scoped to the current user (others' data → `404`); enums as strings in JSON; `tests/TodoApi.Tests` with 43 passing service and endpoint tests | API, tests |
| Phase 4 — Task Details API | Planned | — | Subtasks, notes, reminders, tags endpoints; Postman collection | API |
| Phase 5 — Angular Client | Planned | — | Models, services, pages, forms | client |
| Phase 6 — Polish & Delivery | Planned | — | Styling, README, remote, CI | API, client |

## Open issues

- Refresh token is returned in the JSON body; revisit storage (httpOnly cookie vs. memory) in Phase 5.

## Next steps

- Phase 4: subtasks, notes, reminders, tags endpoints (reuse the DTOs created in Phase 3) and the Postman collection.

## Local setup

```bash
# one-time: JWT signing key (user secrets, never committed) — at least 32 bytes
dotnet user-secrets set "Jwt:SigningKey" "<random-base64-64-bytes>" --project src/TodoApi

dotnet ef database update --project src/TodoApi   # creates LocalDB database TodoApp and seeds dev data
dotnet run --project src/TodoApi                   # http://localhost:5055
dotnet test TodoApplication.slnx                   # uses a throwaway LocalDB database, dropped after the run
```

Dev login: `demo@todo.local` / `Demo123!`
