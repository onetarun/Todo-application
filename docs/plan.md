# Todo Application — Implementation Plan

Status legend: `Planned` · `In Progress` · `Done` · `Blocked`

## Goal

A full-stack Todo app where registered users organize tasks into todo lists. Each task can have
subtasks, notes, reminders, and tags, plus a status, priority, and due date.
Backend: ASP.NET Core Web API (.NET 10, controllers). Frontend: Angular 22. Persistence: EF Core.

## Open Decisions

| # | Decision | Options | Status |
|---|----------|---------|--------|
| 1 | Database provider | SQLite (simple, file-based) · SQL Server · PostgreSQL | **SQL Server** (LocalDB `MSSQLLocalDB` for development) |
| 2 | Git remote | GitHub · GitLab · Azure DevOps | **GitHub** (`https://github.com/onetarun/Todo-application`) |
| 3 | Authentication mechanism | User accounts required (diagram has `passwordHash`). ASP.NET Core Identity · custom JWT with hashed passwords · external provider | **Custom JWT** with refresh tokens (rotation, stored hashed); passwords hashed with `PasswordHasher<User>`; `User` table as in diagram |
| 4 | `Task.status` values | Enum stored as string, e.g. `Todo` · `InProgress` · `Done` | **`Todo` · `InProgress` · `Done`**, stored as string |
| 5 | `Task.priority` range | e.g. `1` (low) – `3` (high) · `1`–`5` | **`1` (low) – `3` (high)**, enforced by check constraint |
| 6 | Tag ownership | Global (as in diagram, no `userId`) · per-user (add `userId` FK) | **Global** (as in diagram) |
| 7 | Reminder delivery | Storage only · in-app notification · email | **In-app notification** while the app is open (v1, Phase 6); no OS/email notifications |

## Data Model

Source: class diagram (2026-10-20).

| Entity | Fields | Relationships |
|--------|--------|---------------|
| `User` | `Id` (PK), `Name`, `Email`, `PasswordHash`, `CreatedAt` | owns 0..* `TodoList` |
| `TodoList` | `Id` (PK), `UserId` (FK), `Title`, `Description`, `CreatedAt` | belongs to 1 `User`; contains 0..* `Task` |
| `Task` | `Id` (PK), `TodoListId` (FK), `Title`, `Description`, `Status`, `Priority`, `DueDate`, `CreatedAt`, `UpdatedAt` | belongs to 1 `TodoList`; has 0..* `Subtask`, `Note`, `Reminder`; tagged via `TaskTag` |
| `Subtask` | `Id` (PK), `TaskId` (FK), `Title`, `IsCompleted` | belongs to 1 `Task` |
| `Note` | `Id` (PK), `TaskId` (FK), `Content`, `CreatedAt` | belongs to 1 `Task` |
| `Reminder` | `Id` (PK), `TaskId` (FK), `RemindAt` | belongs to 1 `Task` |
| `Tag` | `Id` (PK), `Name`, `Color` | used in 0..* `TaskTag` |
| `TaskTag` | `Id` (PK), `TaskId` (FK), `TagId` (FK) | join entity for `Task` ↔ `Tag` (many-to-many) |
| `RefreshToken` *(Phase 2, not in diagram)* | `Id` (PK), `UserId` (FK), `TokenHash`, `ExpiresAt`, `CreatedAt`, `RevokedAt` | belongs to 1 `User` |

Implementation notes:
- Class name `Task` collides with `System.Threading.Tasks.Task`; name the entity `TodoTask` (table `Tasks`) to avoid ambiguity in async code.
- Unique index on `User.Email`; unique index on `TaskTag (TaskId, TagId)`.
- Cascade delete: `User` → `TodoList` → `Task` → `Subtask` / `Note` / `Reminder` / `TaskTag`; deleting a `Tag` removes its `TaskTag` rows.
- `Description` and `DueDate` are optional; all other fields required.

## Phase 0 — Project Setup — `Done`

- [x] Solution `TodoApplication.slnx` with `src/TodoApi` (controller-based Web API)
- [x] Angular client in `client/`
- [x] `CLAUDE.md`, `.claude/settings.json`, `.gitignore`, git repository

## Phase 1 — Backend Foundation — `Done`

- [x] Remove `WeatherForecast` sample (controller, model, `TodoApi.http` request)
- [x] Add `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Design` (10.0.x); create `Data/TodoDbContext.cs`
  (applies configurations via `ApplyConfigurationsFromAssembly`); connection string `ConnectionStrings:TodoDb` in
  `appsettings.Development.json` (LocalDB, Windows auth — no secrets)
- [x] Entities in `Entities/`, one `IEntityTypeConfiguration<T>` per entity in `Data/Configurations/`:

  | Entity | Configuration |
  |--------|---------------|
  | `User` | `Name` max 100; `Email` max 256, unique index; `PasswordHash` required |
  | `TodoList` | `Title` max 200; `Description` max 2000, optional; FK → `User`, cascade |
  | `TodoTask` (table `Tasks`) | `Title` max 200; `Description` max 2000, optional; `Status` enum `TodoTaskStatus` stored as string (max 20); `Priority` int, check constraint 1–3; `DueDate` optional; FK → `TodoList`, cascade |
  | `Subtask` | `Title` max 200; `IsCompleted`; FK → `Task`, cascade |
  | `Note` | `Content` max 4000; `CreatedAt`; FK → `Task`, cascade |
  | `Reminder` | `RemindAt`; FK → `Task`, cascade |
  | `Tag` | `Name` max 50; `Color` max 7 (`#RRGGBB`) |
  | `TaskTag` | own `Id` PK; unique index `(TaskId, TagId)`; cascade from `Task` and `Tag` |

  All timestamps are UTC `DateTime` (`datetime2`). Global tags mean no multiple-cascade-path conflict on SQL Server.
- [x] Initial migration `InitialCreate`; apply with `dotnet ef database update`
- [x] Development seed data in `Data/DevelopmentSeeder.cs` via `UseSeeding` + `UseAsyncSeeding` (both required; tooling
  uses the sync one), registered only in Development; idempotent (skips if any user exists). Seeds user
  `demo@todo.local` (password `Demo123!`, hashed), two lists, several tasks with subtasks/notes/a reminder, three tags
- [x] CORS: named policy reading `Cors:AllowedOrigins` (array) — `http://localhost:4200` in
  `appsettings.Development.json`, empty in `appsettings.json`
- [x] Global error handling: `AddProblemDetails()` + `UseExceptionHandler()` + `UseStatusCodePages()`
- [x] Create `PROGRESS.md` (CLAUDE.md §12)

Verification (2026-10-20, all passed): `dotnet build`; migration applied to LocalDB `TodoApp`; seed rows queried
(1 user, 2 lists, 4 tasks, 4 subtasks, 1 note, 1 reminder, 3 tags, 4 task-tags) and re-run adds nothing; priority `5`
rejected by `CK_Tasks_Priority`; API runs, `/openapi/v1.json` returns `200`; CORS preflight from `http://localhost:4200`
allowed, other origins get no `Access-Control-Allow-Origin`; unknown route returns `404` `application/problem+json`.
Exception → `500` ProblemDetails is verified in Phase 3 (no endpoints exist yet). No test project in this phase — no
business logic yet.

Resolved: build warnings NU1902/NU1903 from transitive `System.Security.Cryptography.Pkcs` 9.0.x (via
`Microsoft.Data.SqlClient`). EF Core packages upgraded to 10.0.13 and `System.Security.Cryptography.Pkcs` 10.0.13 pinned
directly; build is 0 warnings.

## Phase 2 — Authentication — `Done`

Decisions (2026-10-20): access + refresh tokens · register returns `AuthResponse` (signs the user in) · add
`GET /api/auth/me` · automated tests deferred to Phase 3 (Phase 2 verified with scripted HTTP calls).

Proposed contract (locked once implemented — see CLAUDE.md §10):

| Method | Route | Auth | Request | Response |
|--------|-------|------|---------|----------|
| POST | `/api/auth/register` | anonymous | `RegisterRequest` | `201` `AuthResponse` · `400` · `409` |
| POST | `/api/auth/login` | anonymous | `LoginRequest` | `200` `AuthResponse` · `400` · `401` |
| POST | `/api/auth/refresh` | anonymous | `RefreshRequest` | `200` `AuthResponse` · `400` · `401` |
| POST | `/api/auth/logout` | anonymous | `RefreshRequest` | `204` |
| GET | `/api/auth/me` | required | — | `200` `UserDto` · `401` |

DTOs (data-annotation validation):
- `RegisterRequest`: `Name` (required, 1–100), `Email` (required, email, max 256), `Password` (required, 8–100)
- `LoginRequest`: `Email`, `Password` · `RefreshRequest`: `RefreshToken`
- `UserDto`: `Id`, `Name`, `Email`
- `AuthResponse`: `AccessToken`, `AccessTokenExpiresAt`, `RefreshToken`, `RefreshTokenExpiresAt`, `User` (`UserDto`)

Behavior:
- Access token: HS256 JWT, 15 min; claims `sub` (user id), `email`, `name`; created with `JsonWebTokenHandler`
- Refresh token: 64 random bytes, 7 days; only its SHA-256 hash stored (`RefreshTokens` table)
- Rotation: each `/refresh` revokes the old token and issues a new pair; reuse of a revoked token revokes all of that
  user's refresh tokens (likely theft)
- `/logout` revokes the given refresh token; always `204` (no token existence leak)
- Passwords verified with `IPasswordHasher<User>`; rehashed on `SuccessRehashNeeded`
- Emails trimmed and lowercased before storing/lookup
- Unknown email and wrong password return the same `401` ProblemDetails; duplicate email on register → `409`
- Refresh token travels in the JSON body (simple for the client). Trade-off: if the client keeps it in `localStorage`,
  XSS could steal it; an httpOnly cookie is safer but needs CORS `AllowCredentials` + CSRF protection — revisit in Phase 5

Tasks:
- [x] Add `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.13)
- [x] `Entities/RefreshToken.cs` + `Data/Configurations/RefreshTokenConfiguration.cs` (FK → `User`, cascade; unique
  index on `TokenHash`); migration `AddRefreshTokens`
- [x] `Auth/JwtOptions.cs` bound to `Jwt` section (`Issuer`, `Audience`, `SigningKey`, `AccessTokenMinutes`,
  `RefreshTokenDays`), validated on start — app fails fast if the signing key is missing or under 32 bytes
- [x] `Auth/ITokenService` / `TokenService` (access token creation, refresh token generation and hashing)
- [x] `Auth/ICurrentUser` / `CurrentUser` (user id from `sub`) for Phase 3 per-user query scoping
- [x] `Services/IAuthService` / `AuthService` (register, login, refresh, logout, me); controller maps results to status codes
- [x] DTOs in `Dtos/Auth/`
- [x] `Controllers/AuthController.cs` (thin, async, `CancellationToken`; `[AllowAnonymous]` except `/me`)
- [x] `Program.cs`: `AddAuthentication().AddJwtBearer(...)` validating issuer, audience, signing key, lifetime
  (`MapInboundClaims = false`); fallback authorization policy requiring an authenticated user (new controllers are
  protected by default); `MapOpenApi().AllowAnonymous()`; middleware order CORS → authentication → authorization.
  Side effect: anonymous requests to unknown routes return `401` instead of `404`
- [x] Config: `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenMinutes` (15), `Jwt:RefreshTokenDays` (7) in
  `appsettings.json`; `Jwt:SigningKey` only in user secrets for development (`dotnet user-secrets init` / `set`) and
  environment variable `Jwt__SigningKey` in production — never in source control
- [x] Update `PROGRESS.md`

Verification: build with 0 warnings; `AddRefreshTokens` applied; scripted HTTP checks — register `201`, duplicate `409`,
invalid `400`; seed user login `200`, wrong password `401`; `/me` with token `200`, without `401`; refresh returns a new
pair, reusing the old refresh token `401` and revokes all the user's tokens; logout `204` and that token stops working;
`/openapi/v1.json` still `200`; CORS preflight still allowed; database stores token hashes only.

Verification result (2026-10-20): build 0 warnings / 0 errors; `AddRefreshTokens` applied; scripted HTTP checks
24/24 passed (also: email normalized on register, duplicate check case-insensitive, tampered token `401`, `401`s are
`application/problem+json`); `RefreshTokens.TokenHash` holds 64-char SHA-256 hex only; startup with a short
`Jwt:SigningKey` fails with `OptionsValidationException`.

~~Known limitation: concurrent duplicate registration surfaced as `500`.~~ Fixed in Phase 3: `AuthService` maps SQL
Server unique-key violations (2601/2627) to `409`; covered by an 8-way concurrent registration test.

## Phase 3 — Lists & Tasks API — `Done`

Proposed contract (locked once implemented — see CLAUDE.md §10). All routes require authentication. Auth endpoints are
in Phase 2.

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/lists` | — | `200` `TodoListDto[]` |
| GET | `/api/lists/{id}` | — | `200` `TodoListDto` · `404` |
| POST | `/api/lists` | `CreateTodoListRequest` | `201` `TodoListDto` · `400` |
| PUT | `/api/lists/{id}` | `UpdateTodoListRequest` | `200` `TodoListDto` · `400` · `404` |
| DELETE | `/api/lists/{id}` | — | `204` · `404` |
| GET | `/api/lists/{listId}/tasks` | query: `status?`, `priority?`, `tagId?` | `200` `TaskDto[]` · `404` |
| POST | `/api/lists/{listId}/tasks` | `CreateTaskRequest` | `201` `TaskDto` · `400` · `404` |
| GET | `/api/tasks/{id}` | — | `200` `TaskDetailDto` · `404` |
| PUT | `/api/tasks/{id}` | `UpdateTaskRequest` | `200` `TaskDto` · `400` · `404` |
| PATCH | `/api/tasks/{id}/status` | `UpdateTaskStatusRequest` | `204` · `400` · `404` |
| DELETE | `/api/tasks/{id}` | — | `204` · `404` |

- [x] DTOs with validation for lists and tasks (`TaskDetailDto` includes subtasks, notes, reminders, tags)
- [x] `ITodoListService` / `ITaskService` holding business logic
- [x] `TodoListsController`, `TasksController` (thin, async, `CancellationToken`); every query scoped via `ICurrentUser`
- [x] Create test project `tests/TodoApi.Tests`; unit tests (services, including `AuthService`) and integration tests
  (endpoints, including Phase 2 auth endpoints)

Implementation notes (2026-10-20):
- DTOs: `TodoListDto(Id, Title, Description, CreatedAt)`; `TaskDto(Id, TodoListId, Title, Description, Status, Priority,
  DueDate, CreatedAt, UpdatedAt, Tags)`; `TaskDetailDto` = `TaskDto` fields + `Subtasks`, `Notes`, `Reminders`.
  `TagDto`, `SubtaskDto`, `NoteDto`, `ReminderDto` were created now for `TaskDetailDto` and will be reused in Phase 4.
- `CreateTaskRequest`: `Status` defaults to `Todo`, `Priority` defaults to `2`. `UpdateTaskRequest` is a full replace
  (`Status` and `Priority` required). Titles are trimmed.
- JSON enums are names only (`"InProgress"`); numeric enum values are rejected with `400`.
- Another user's lists and tasks return `404` (not `403`), so their existence is not revealed.
- Deletes rely on the database cascades; task `UpdatedAt` is set on PUT and PATCH status.
- Tests: xUnit, one throwaway LocalDB database per run (`TodoApp_Tests_<guid>`, migrated, dropped afterwards); app runs
  in a `Testing` environment (no seed, no user secrets); `FakeTimeProvider` for timestamps and refresh-token expiry.

Verification (2026-10-20): build 0 warnings / 0 errors; no pending model changes (no migration needed);
`dotnet test` 43/43 passed — services (lists, tasks, auth), endpoints (auth, lists, tasks, per-user isolation,
validation, filters), concurrent duplicate registration → one `201` and the rest `409`, and an unhandled exception
(database unreachable) → `500` `application/problem+json` without internal details. A test run found and fixed one
bug: revoking all refresh tokens on reuse left already-loaded tokens stale within the same `DbContext`.

## Phase 4 — Task Details API — `Done`

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/tasks/{taskId}/subtasks` | — | `200` `SubtaskDto[]` · `404` |
| POST | `/api/tasks/{taskId}/subtasks` | `CreateSubtaskRequest` | `201` `SubtaskDto` · `400` · `404` |
| PUT | `/api/subtasks/{id}` | `UpdateSubtaskRequest` | `200` `SubtaskDto` · `400` · `404` |
| PATCH | `/api/subtasks/{id}/complete` | — | `204` · `404` |
| DELETE | `/api/subtasks/{id}` | — | `204` · `404` |
| GET | `/api/tasks/{taskId}/notes` | — | `200` `NoteDto[]` · `404` |
| POST | `/api/tasks/{taskId}/notes` | `CreateNoteRequest` | `201` `NoteDto` · `400` · `404` |
| PUT | `/api/notes/{id}` | `UpdateNoteRequest` | `200` `NoteDto` · `400` · `404` |
| DELETE | `/api/notes/{id}` | — | `204` · `404` |
| GET | `/api/tasks/{taskId}/reminders` | — | `200` `ReminderDto[]` · `404` |
| POST | `/api/tasks/{taskId}/reminders` | `CreateReminderRequest` | `201` `ReminderDto` · `400` · `404` |
| DELETE | `/api/reminders/{id}` | — | `204` · `404` |
| GET | `/api/tags` | — | `200` `TagDto[]` |
| POST | `/api/tags` | `CreateTagRequest` | `201` `TagDto` · `400` |
| PUT | `/api/tags/{id}` | `UpdateTagRequest` | `200` `TagDto` · `400` · `404` |
| DELETE | `/api/tags/{id}` | — | `204` · `404` |
| PUT | `/api/tasks/{taskId}/tags/{tagId}` | — | `204` · `404` |
| DELETE | `/api/tasks/{taskId}/tags/{tagId}` | — | `204` · `404` |

- [x] DTOs, services, and controllers for subtasks, notes, reminders, tags
- [x] Unit and integration tests
- [x] Postman collection covering the full contract (Phases 3–4)

Implementation notes (2026-10-20):
- Requests: `CreateSubtaskRequest(Title)`, `UpdateSubtaskRequest(Title, IsCompleted)` (full replace, both required);
  `CreateNoteRequest` / `UpdateNoteRequest(Content)` (1–4000); `CreateReminderRequest(RemindAt)` (required);
  `CreateTagRequest` / `UpdateTagRequest(Name, Color)` (name 1–50, color `#RRGGBB`, stored upper case). Text is trimmed.
- Services `ISubtaskService`, `INoteService`, `IReminderService`, `ITagService`; controllers `SubtasksController`,
  `NotesController`, `RemindersController`, `TagsController`. Subtasks, notes, reminders and tag assignments are
  scoped to the current user through the task's list — another user's data returns `404`.
- `201` responses: the contract has no single-item GET for subtasks, notes, reminders or tags, so `Location` points
  at the collection (e.g. `/api/tasks/{taskId}/subtasks`, `/api/tags`).
- `PATCH /api/subtasks/{id}/complete` only sets `IsCompleted = true`; un-completing goes through `PUT`.
- Tag assignment `PUT`/`DELETE /api/tasks/{taskId}/tags/{tagId}` is idempotent (`204` whether or not it was already
  assigned); `404` only when the task (for this user) or the tag does not exist. A concurrent duplicate assignment is
  treated as success.
- Tags are global (decision 6): any signed-in user can rename or delete any tag, and deleting one removes it from
  every user's tasks. Tag names are not unique. Reminders are stored only (decision 7 still open); past dates are
  accepted.
- Shared helper `DbUpdateExceptionExtensions.IsUniqueConstraintViolation()` (moved out of `AuthService`).
- No schema change; no migration.
- Postman: `postman/TodoApi.postman_collection.json` (v2.1, 57 requests in run order: Auth → Lists → Tasks → Subtasks
  → Notes → Reminders → Tags → Cleanup) and `postman/TodoApi.local.postman_environment.json` (`baseUrl`
  `http://localhost:5055`). It registers a fresh `postman.<timestamp>@test.local` user each run and deletes the data
  it creates (the user remains).

Verification (2026-10-20): build 0 warnings / 0 errors; no pending model changes; `dotnet test` 73/73 passed (30 new:
service tests for subtasks, notes, reminders, tags; endpoint tests for every Phase 4 route, validation, per-user
isolation, idempotent tag assignment, tag delete cascade, auth required). `postman collection run` against the local
API: 57 requests, 91 assertions, 0 failures. OpenAPI document lists all Phase 4 routes.

## Phase 5 — Angular Client — `Done`

Decisions (2026-10-20):
- **Refresh token storage:** `localStorage` (no backend change). Access token kept in memory only; on page load the
  stored refresh token is exchanged for a new access token. Trade-off accepted: script-readable (XSS), mitigated by
  15-minute access tokens and refresh-token rotation with reuse detection.
- **UTC dates:** fixed in the backend. An EF Core value converter marks every `DateTime` read from the database as
  UTC, so the API serializes `dueDate`, `remindAt`, `createdAt`, `updatedAt` with a `Z`. Same fields and types —
  approved contract clarification (CLAUDE.md §10). The client models keep dates as ISO strings.
- **UI library:** Angular Material + CDK (`npx ng add @angular/material`, version matching Angular 22). Plain CSS
  only for layout; full styling polish stays in Phase 6. The production initial-bundle budget (500 kB warning) may
  need raising — report the actual size.
- **Forms:** Reactive Forms (per CLAUDE.md §6), not Signal Forms.

Backend prerequisite:
- [x] UTC `DateTime` value converter in `TodoDbContext`; test that dates come back with `Z`; Postman run still passes

Client setup:
- [x] `provideHttpClient(withInterceptors([...]))`; `proxy.conf.json` (`/api` → `http://localhost:5055`) wired into
  the `serve` target; `API_BASE_URL` injection token (no hard-coded URLs in services)
- [x] Angular Material added (theme, typography, animations as required by `ng add`)

Models (`src/app/models/`) — mirror the backend DTOs exactly:
- [x] Auth (`RegisterRequest`, `LoginRequest`, `RefreshRequest`, `AuthResponse`, `User`), `TodoList`, `Task`,
  `TaskDetail`, `Subtask`, `Note`, `Reminder`, `Tag`, and every request model; `TaskStatus = 'Todo' | 'InProgress' | 'Done'`

Core (`src/app/core/`):
- [x] `AuthService`: register, login, logout, `me`, refresh; `currentUser` signal; token storage as decided above
- [x] `authInterceptor`: adds `Bearer` to `/api` calls (not auth endpoints); on `401` refreshes once (concurrent
  requests share one refresh) and retries; on refresh failure signs out and redirects to `/login`
- [x] `authGuard` for app routes; `guestGuard` keeps signed-in users away from `/login` and `/register`

Services (`src/app/services/`):
- [x] `TodoListService`, `TaskService`, `SubtaskService`, `NoteService`, `ReminderService`, `TagService` — thin
  `HttpClient` wrappers returning Observables

Pages (lazy-loaded, standalone, `OnPush`, signals, `@if`/`@for`, Reactive Forms with validation messages):

| Route | Page |
|-------|------|
| `/login`, `/register` | Auth forms; show server errors (`409` duplicate email, `401` bad credentials) |
| `/lists` | Lists: view, create, rename, delete |
| `/lists/:listId` | Tasks in a list: filter by status / priority / tag; quick status change; create; delete |
| `/tasks/:taskId` | Task detail: edit task; manage subtasks, notes, reminders, and tag assignment |
| `/tags` | Manage global tags (name, color) |

- [x] Login / register pages
- [x] Lists page
- [x] Tasks view
- [x] Task detail
- [x] Tags page
- [x] Loading, empty, and error states on every data view; errors shown from ProblemDetails
- [x] Accessibility: labelled controls, keyboard-operable interactions, `aria-live` for errors

Tests (Vitest via `ng test`):
- [x] Services and interceptor with `HttpTestingController` (including refresh-and-retry and refresh failure)
- [x] Guards
- [x] Key components: login form, lists page, task detail

Verification: `ng build` and `ng test` pass; `dotnet test` and the Postman run still pass after the date fix; manual
run against the API covering sign-in → lists → tasks → task details.

Implementation notes (2026-10-20):
- Backend: `Data/UtcDateTimeConverter` applied to every `DateTime` (including nullable) via `ConfigureConventions`.
  Values without an offset are taken as UTC, local values are converted. No schema change, no migration.
- Angular 22 conventions followed: `@Service()` (provided in root) for services; components rely on the framework
  default change detection, which is `OnPush` in Angular 22.
- Material `22.2.3` via `ng add` (Material 3 azure theme in `src/material-theme.scss`, Roboto and Material Symbols
  from Google Fonts in `index.html`). Icons use the `material-symbols-outlined` font set.
- Data loading uses `rxResource` over the services' Observables (loading / error / value signals; `reload()` after
  every change). Route params bind to component inputs (`withComponentInputBinding`).
- Session: `provideAppInitializer` restores the session from the stored refresh token before the first navigation;
  concurrent refreshes share one request (the server rotates refresh tokens and treats reuse as theft). Two browser
  tabs refreshing at the same moment can still trigger reuse detection and sign both out.
- Due dates are calendar days: sent as UTC midnight, shown with the `UTC` time zone. Reminders are instants: entered
  in local time (`datetime-local`), sent as UTC, shown in local time.
- Task detail keeps unsaved edits in the details form when another section (subtasks, notes, ...) reloads the task.
- `PATCH /api/subtasks/{id}/complete` is used to tick a subtask; unticking uses `PUT` (the contract has no "uncomplete").
- Login only follows in-app `returnUrl` values (no open redirect).
- `src/testing/test-data.ts` holds shared test payloads (excluded from the app build).

Verification (2026-10-20): backend build 0 warnings; no pending model changes; `dotnet test` 74/74 (1 new: dates
serialize with `Z`); Postman run 57 requests / 91 assertions passing after the date fix. Client `ng build` with no
warnings — initial bundle 452.5 kB raw / 111.9 kB transfer (under the 500 kB budget, so unchanged); `ng test` 51/51
(18 files). Live check with the API and `ng serve` running: the dev proxy forwards `/api` (sign-in as the demo user
through `http://localhost:4200/api` succeeded). **Not done:** a click-through in a real browser — no browser
automation was available in this session; to be checked by the user.

## Phase 6 — Polish & Delivery — `Done`

Decisions (2026-10-20):
- **CI:** GitHub Actions on every push and pull request. API job on `windows-latest` (tests keep using LocalDB —
  confirm it is on the image, otherwise start it in a step; no test-code changes); client job on `ubuntu-latest`.
- **Theme:** follow the OS light/dark setting (`color-scheme: light dark`); no toggle.
- **Reminders:** in-app notifications while the app is open (closes decision 7). Requires one new, additive endpoint
  (approved; CLAUDE.md §10).
- **Accessibility:** manual review against the checklist below; no new packages.

Styling and accessibility:
- [x] Responsive layout: toolbar collapses to a menu on narrow screens; tasks list and task detail stack cleanly
  down to 360 px; touch targets ≥ 44 px
- [x] Visual polish: consistent spacing; priority/status colors meet WCAG AA contrast and always carry a text label
- [x] Theme follows the OS light/dark setting; custom colors checked in both modes
- [x] Focus moves to the page heading on navigation (announced by screen readers); visible focus styles; every
  icon-only button labelled; errors announced (`role="alert"`); `prefers-reduced-motion` respected

Reminder notifications:

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/reminders` | query: `before` (UTC, required) | `200` `DueReminderDto[]` · `400` |

- [x] Backend: `DueReminderDto(Id, TaskId, TaskTitle, RemindAt)`; returns the current user's reminders with
  `RemindAt` between `before − 24 h` and `before`, newest first; scoped through the task's list; no schema change
- [x] Backend tests (service + endpoint, per-user isolation, 24-hour window, validation) and a Postman request
- [x] Client: `DueReminder` model; `ReminderService.getDue(before)`; `ReminderNotifier` runs while signed in — checks
  on sign-in and every 60 s, shows each due reminder once as a snackbar ("Reminder: <task title>") with an **Open**
  action to the task; shown reminder ids kept in `localStorage` (per browser, not per account)
- [x] Client tests for the notifier (shows once, skips already-shown, stops on sign-out)
- Limitations (documented in the README): only while the app is open in a tab; up to ~1 minute late; reminders
  missed by more than 24 hours are not shown.

README:
- [x] Root `README.md`: overview, stack, prerequisites (.NET 10 SDK, SQL Server LocalDB, Node), one-time setup
  (JWT signing key in user secrets, `dotnet ef database update`), running API and client, running tests and the
  Postman collection, API overview, project layout, key decisions (token storage, global tags, UTC dates,
  reminders), CI badge
- [x] `client/README.md`: replace the Angular boilerplate with a short pointer to the root README

Delivery:
- [x] Push to remote (GitHub, decision 2)
- [x] CI workflow `.github/workflows/ci.yml`: API (`windows-latest`: .NET 10, LocalDB, `dotnet build`,
  `dotnet test`) and client (`ubuntu-latest`: Node, `npm ci`, `ng build`, `ng test`)

Verification: backend build and `dotnet test`, client `ng build` and `ng test`, Postman run all pass; CI green on
GitHub; layout checked at phone and desktop widths. The browser click-through (carried over from Phase 5) and the
responsive/keyboard checks need a browser — done by Claude if the Claude in Chrome extension is enabled, otherwise
by the user.

Implementation notes (2026-10-20):
- Reminders endpoint: `GET /api/reminders?before=` (`DueReminderQuery`, `DueReminderDto`), window
  `(before − 24 h, before]` (`ReminderService.DueWindow`), newest first, scoped through the task's list. An
  offset-less `before` is taken as UTC (reuses `UtcDateTimeConverter.ToUtc`). Additive; no schema change.
- `ReminderNotifier` (client `core/`): started by an app initializer; polls on sign-in and every 60 s
  (`exhaustMap`, so slow responses never overlap), failed checks are skipped silently, stops on sign-out. Shows due
  reminders oldest first, one snackbar at a time (10 s, `politeness: 'assertive'`), **Open** navigates to the task;
  shown ids in `localStorage` (`todo.shownReminders`, last 200).
- Toolbar: inline navigation on wide screens, a menu button (Lists, Tags, Sign out) at ≤ 600 px.
- Focus: after each in-app navigation focus moves to the page's `h1` (or `main` while the page is still loading);
  the first page load keeps the browser's default focus. Strong focus indicators enabled for Material components;
  `:focus-visible` outline for links, the color input and headings.
- Theme follows the OS (`color-scheme: light dark`); links use the theme's primary color (readable in dark mode);
  tag swatches have an outline so light/dark tag colors stay visible.
- Reduced motion: Material 22 already disables its animations when the OS requests reduced motion; a global CSS
  rule covers anything else.
- Accessibility review: every icon-only button has an `aria-label` (checked by script); per-task status selects
  include the task title as visually hidden label text; errors use `role="alert"`; headings are `h1` → `h2`.
- Bundle budget: the toolbar menu and the reminder snackbar bring Material's overlay into the initial bundle —
  554.65 kB raw / 132.38 kB transfer. The initial warning budget was raised from 500 kB to 600 kB (error stays 1 MB),
  as allowed by the Phase 5 decision.
- README: root `README.md` (setup, run, test, API overview, layout, design notes, CI badge); `client/README.md` now
  points to it.
- CI: `.github/workflows/ci.yml`. LocalDB is not listed as a standalone tool on the GitHub Windows images but is
  installed with Visual Studio (`Microsoft.VisualStudio.Component.SQL.LocalDB.Runtime` on both the 2022 and 2025
  images), so the workflow only starts the `MSSQLLocalDB` instance.

Verification (2026-10-20): backend build 0 warnings; no pending model changes; `dotnet test` 77/77 (3 new: due
reminders window/isolation, offset-less `before`, endpoint incl. `400`s). Postman run 59 requests / 95 assertions, 0
failures (2 new requests). Client `ng build` with no warnings after the budget change; `ng test` 56/56 (19 files; new:
notifier ×5, `getDue`). Live check with the API and `ng serve`: a reminder due one minute earlier was returned through
the dev proxy with its task title, then removed. **Not yet verified:** CI has not run — it runs on the next push to
GitHub; the browser click-through, phone-width layout and keyboard checks were not done (no browser automation in
this session).

## Progress Log

| Date | Phase | Change |
|------|-------|--------|
| 2026-10-20 | 0 | Project scaffolded, guidelines and git initialized |
| 2026-10-20 | — | Plan updated to class diagram: users, lists, tasks, subtasks, notes, reminders, tags |
| 2026-10-20 | 1 | Decisions 1, 3–6 resolved (SQL Server, custom JWT, status enum, priority 1–3, global tags); Phase 1 detailed |
| 2026-10-20 | 1 | Phase 1 done: EF Core + SQL Server, 8 entities, `InitialCreate` migration, dev seed, CORS, ProblemDetails |
| 2026-10-20 | 1 | Package vulnerability warnings fixed: EF Core 10.0.13, `System.Security.Cryptography.Pkcs` 10.0.13 pinned |
| 2026-10-20 | 2 | Phase 2 detailed: JWT + rotating refresh tokens, register/login/refresh/logout/me contract; auth rows moved from Phase 3 |
| 2026-10-20 | 2 | Phase 2 done: JWT bearer auth, rotating refresh tokens, `AuthController`, fallback auth policy, `AddRefreshTokens` migration |
| 2026-10-20 | 3 | Phase 3 done: lists & tasks API with per-user scoping, `tests/TodoApi.Tests` (43 tests), registration race fixed |
| 2026-10-20 | 4 | Phase 4 done: subtasks, notes, reminders, tags endpoints (73 tests); Postman collection for the full contract (57 requests passing) |
| 2026-10-20 | 5 | Phase 5 detailed: refresh token in `localStorage`, UTC date fix in backend, Angular Material, routes and test scope |
| 2026-10-20 | 5 | Phase 5 done: UTC date fix (74 API tests), Angular client with Material — auth, lists, tasks, task detail, tags (51 client tests) |
| 2026-10-20 | 6 | Phase 6 detailed: CI (Windows API job), system theme, in-app reminder notifications (decision 7 closed), README |
| 2026-10-20 | 6 | Phase 6 done: in-app reminders (`GET /api/reminders`), responsive toolbar, system theme, focus management, README, CI workflow (77 API / 56 client tests) |
