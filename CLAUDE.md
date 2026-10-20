# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## 1. Project Overview

A full-stack Todo application: an ASP.NET Core Web API serves Todo data over REST, and an Angular single-page app consumes it.

```
Todo-application/
├── CLAUDE.md
├── TodoApplication.slnx      # .NET solution (XML slnx format)
├── src/
│   └── TodoApi/              # ASP.NET Core Web API (controller-based)
└── client/                   # Angular SPA
```

## 2. Claude's Role in the Project

- Act as a pair-programmer for both backend and frontend: design, implement, review, and explain.
- **Staged approval:** propose a plan first; approval of a plan is *not* permission to create or modify files. Wait for an explicit "create" / "go ahead" before writing code.
- Do only what was asked. Do not add features, packages, files, or refactors beyond the request — suggest them instead.
- Ask when a decision belongs to the user (naming, database provider, UX choices); otherwise follow the conventions in this file.
- Report results honestly: if a build or test fails or a step was skipped, say so with the output.

## 3. Project Technology

| Area | Technology |
|------|------------|
| Backend | .NET 10, ASP.NET Core Web API (controllers) |
| Solution | `TodoApplication.slnx` |
| Frontend | Angular 22, TypeScript 6, standalone components, CSS |
| Frontend build/test | `@angular/build` (application, dev-server, unit-test) |
| Database | Entity Framework Core (provider to be decided) |
| API testing | Postman CLI |

## 4. Backend Rules (`src/TodoApi`)

- Use controller-style APIs (`[ApiController]`, `ControllerBase`) — never minimal APIs.
- Keep controllers thin: validation + delegation. Business logic lives in services registered via DI.
- Never expose EF entities from controllers; use request/response DTOs.
- Use `async`/`await` end-to-end and pass `CancellationToken` through to data access.
- Return proper status codes (`200`, `201` with `CreatedAtAction`, `204`, `400`, `404`) and `ProblemDetails` for errors.
- Validate input with data annotations or a dedicated validator; never trust client input.
- Configuration via `appsettings*.json` / environment variables; no secrets in source control.
- Enable CORS only for the Angular dev origin (`http://localhost:4200`) and configured production origins.

## 5. Front-end Rules (`client`)

- All API calls go through Angular services — components never call `HttpClient` directly.
- Keep the API base URL in configuration (environment file or dev proxy), never hard-coded in services.
- TypeScript models in the client must mirror the backend DTOs exactly (see §10).
- Handle loading, empty, and error states for every data-driven view.
- Keep UI accessible: semantic HTML, labels on form controls, keyboard-operable interactions.

## 6. Angular Rules

- Standalone components only — no `NgModule`s.
- Prefer signals (`signal`, `computed`, `input()`, `output()`) for component state.
- Use built-in control flow (`@if`, `@for`, `@switch`) — not `*ngIf` / `*ngFor`.
- Use `inject()` for dependency injection.
- Use `ChangeDetectionStrategy.OnPush` for new components.
- Lazy-load feature routes in `app.routes.ts`.
- Use Reactive Forms for forms.
- Strict TypeScript: no `any` unless justified in a comment.
- Generate code with the CLI: `npx ng generate ...` (Angular CLI is not installed globally).

## 7. Database Rules

- Access data only through EF Core `DbContext`; no raw SQL unless parameterized and justified.
- Every schema change goes through an EF Core migration with a descriptive name; never edit an applied migration.
- Do not drop tables/columns or run destructive migrations without explicit user approval.
- Use `AsNoTracking()` for read-only queries; project to DTOs with `Select` instead of loading full entities.
- Connection strings live in configuration / user secrets, never in code.
- Seed data only for development, and keep it idempotent.

## 8. Coding Principles

- Readability over cleverness; follow the naming and style of surrounding code.
- SOLID, single responsibility, small focused methods and components.
- DRY — but don't abstract until there is real duplication.
- YAGNI — build only what the current requirement needs.
- Fail fast with clear errors; never swallow exceptions silently.
- Write tests for business logic and API endpoints; keep existing tests passing.
- Comments explain *why*, not *what*.

## 9. Documentation Reading Rules

- Before using or answering about any library, framework, SDK, or CLI (ASP.NET Core, EF Core, Angular, etc.), fetch current docs with **Context7 MCP** (`resolve-library-id` → `query-docs`) — even for well-known APIs. Training data may be outdated (e.g. Angular 22, .NET 10 changes).
- Prefer official documentation over blog posts or web search.
- Read existing project code and this file before making changes; match established patterns.

## 10. API Contract Protection

- The API contract (routes, HTTP methods, request/response DTO shapes, status codes) is a public interface between backend and frontend.
- Do **not** rename, remove, or change the type/meaning of existing endpoints or DTO fields without explicit user approval.
- Changes must be additive (new optional fields, new endpoints). Breaking changes require a new API version.
- When a DTO changes, update the matching Angular model and service in the same change.
- Keep the OpenAPI document (`/openapi/v1.json`) accurate; verify contract changes with Postman CLI where possible.

## 11. Development Workflows

**Backend** (from repo root):
```bash
dotnet build TodoApplication.slnx
dotnet run --project src/TodoApi
dotnet test TodoApplication.slnx
dotnet sln TodoApplication.slnx add <path>.csproj   # register new projects
dotnet ef migrations add <Name> --project src/TodoApi
```

**Frontend** (from `client/`):
```bash
npm start          # dev server at http://localhost:4200
npm run build
npm test
npx ng generate component <name>
```

**Change workflow:**
1. Understand the request; read relevant code and docs (§9).
2. Propose a plan and wait for explicit approval to create/modify files.
3. Implement in small steps; keep backend and frontend contracts in sync (§10).
4. Build and run tests for every affected project.
5. Summarize what changed, what was verified, and anything left open.

## 12. Progress Tracking

- Track work in `PROGRESS.md` at the repository root (create it when the first feature starts).
- For each feature record: status (`Planned` / `In Progress` / `Done` / `Blocked`), date, short description, and affected areas (API, client, database).
- Update it at the end of each completed task and note any open issues or next steps.
- Use the session task list for multi-step work within a single session.
