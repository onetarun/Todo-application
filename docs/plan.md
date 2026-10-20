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
| 2 | Git remote | GitHub · GitLab · Azure DevOps | Open |
| 3 | Authentication mechanism | User accounts required (diagram has `passwordHash`). ASP.NET Core Identity · custom JWT with hashed passwords · external provider | **Custom JWT** with refresh tokens (rotation, stored hashed); passwords hashed with `PasswordHasher<User>`; `User` table as in diagram |
| 4 | `Task.status` values | Enum stored as string, e.g. `Todo` · `InProgress` · `Done` | **`Todo` · `InProgress` · `Done`**, stored as string |
| 5 | `Task.priority` range | e.g. `1` (low) – `3` (high) · `1`–`5` | **`1` (low) – `3` (high)**, enforced by check constraint |
| 6 | Tag ownership | Global (as in diagram, no `userId`) · per-user (add `userId` FK) | **Global** (as in diagram) |
| 7 | Reminder delivery | Storage only · in-app notification · email | Open |

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

## Phase 4 — Task Details API — `Planned`

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

- [ ] DTOs, services, and controllers for subtasks, notes, reminders, tags
- [ ] Unit and integration tests
- [ ] Postman collection covering the full contract (Phases 3–4)

## Phase 5 — Angular Client — `Planned`

- [ ] `provideHttpClient()` and API base URL config / dev proxy
- [ ] Models mirroring backend DTOs (lists, tasks, subtasks, notes, reminders, tags, auth)
- [ ] Services per resource (HttpClient wrappers); auth interceptor and route guard
- [ ] Login / register pages
- [ ] Lists page: view, create, rename, delete lists
- [ ] Tasks view: list tasks in a list; filter by status / priority / tag; change status; delete
- [ ] Task detail: edit task; manage subtasks, notes, reminders, and tags
- [ ] Add/edit forms (Reactive Forms, validation messages)
- [ ] Loading, empty, and error states
- [ ] Component and service unit tests

## Phase 6 — Polish & Delivery — `Planned`

- [ ] Responsive, accessible styling
- [ ] README with setup and run instructions
- [ ] Push to remote; optional CI (build + test for API and client)

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
