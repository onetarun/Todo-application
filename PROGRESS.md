# Progress

Detailed plan: [docs/plan.md](docs/plan.md) · Setup and usage: [README.md](README.md)

| Feature | Status | Date | Description | Areas |
|---------|--------|------|-------------|-------|
| Phase 0 — Project setup | Done | 2026-10-20 | Solution, Angular client, CLAUDE.md, git | API, client |
| Phase 1 — Backend foundation | Done | 2026-10-20 | EF Core + SQL Server (LocalDB), 8 entities with configuration, `InitialCreate` migration, idempotent dev seed, CORS for `http://localhost:4200`, ProblemDetails error handling; WeatherForecast sample removed | API, database |
| Phase 2 — Authentication | Done | 2026-10-20 | Custom JWT (15 min) + rotating refresh tokens (7 days, stored hashed, reuse revokes all); `/api/auth` register, login, refresh, logout, me; all endpoints require auth by default; `AddRefreshTokens` migration | API, database |
| Phase 3 — Lists & Tasks API | Done | 2026-10-20 | `/api/lists` and `/api/tasks` CRUD, task filters (status, priority, tag), status PATCH; every query scoped to the current user (others' data → `404`); enums as strings in JSON; `tests/TodoApi.Tests` with 43 passing service and endpoint tests | API, tests |
| Phase 4 — Task Details API | Done | 2026-10-20 | Subtasks, notes, reminders, tags endpoints (user-scoped; tags global); idempotent tag assignment; 73 passing tests; Postman collection `postman/` covering the full contract (57 requests passing) | API, tests |
| Phase 5 — Angular Client | Done | 2026-10-20 | Angular Material client: sign in/up, lists, tasks (filters, quick status), task detail (subtasks, notes, reminders, tags), tags page; auth interceptor with refresh-and-retry, guards, dev proxy; 51 client tests. Backend: dates now serialized as UTC (`Z`), 74 API tests | API, client |
| Phase 6 — Polish & Delivery | Done | 2026-10-20 | In-app reminder notifications (`GET /api/reminders?before=`, client notifier); responsive toolbar menu, system light/dark theme, focus management and focus styles; root README; GitHub Actions CI. 77 API tests, 56 client tests, 59 Postman requests passing | API, client |

## Open issues

- Not yet verified: CI has not run yet (it runs on the next push); the UI has not been clicked through in a real
  browser, including phone-width layout and keyboard navigation (no browser automation in the sessions that built it).
- Refresh token is kept in `localStorage` (access token in memory only); accepted XSS trade-off. Two tabs refreshing
  at the same moment can trigger the server's reuse detection and sign the user out.
- Tags are global: any signed-in user can rename or delete any tag (affects every user's tasks); tag names are not unique.
- Reminders are in-app only: shown while the app is open, up to ~1 minute late; missed by more than 24 h → not shown.
- The initial client bundle is 554.65 kB (budget warning raised to 600 kB in Phase 6).

## Next steps

- Push to GitHub and confirm the CI run is green.
- Click through the app in a browser (sign in → lists → tasks → task details → tags → reminder notification), at
  phone and desktop widths and with the keyboard only.

## Local setup

See [README.md](README.md). Dev login: `demo@todo.local` / `Demo123!`
